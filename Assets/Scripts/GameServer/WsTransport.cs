using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

public class WsTransport : INetTransport
{
    private const string WsGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
    private const int MaxFrameBytes = 1024 * 1024;
    private const int OutboxCapacity = 256;

    public event Action<int, string> ClientConnected;
    public event Action<int, string> MessageReceived;
    public event Action<int> ClientDisconnected;

    private TcpListener listener;
    private Thread acceptThread;
    private volatile bool running;
    private int nextId;
    private readonly ConcurrentDictionary<int, Client> clients = new ConcurrentDictionary<int, Client>();
    private readonly ConcurrentQueue<TransportEvent> events = new ConcurrentQueue<TransportEvent>();

    private class Client
    {
        public TcpClient Tcp;
        public NetworkStream Stream;
        public readonly BlockingCollection<byte[]> Outbox = new BlockingCollection<byte[]>(OutboxCapacity);
        public volatile bool Closed;
    }

    private struct TransportEvent
    {
        public int Kind;
        public int ConnId;
        public string Payload;
    }

    public void Start(int port)
    {
        running = true;
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "ws-accept" };
        acceptThread.Start();
    }

    public void Poll()
    {
        while (events.TryDequeue(out TransportEvent e))
        {
            switch (e.Kind)
            {
                case 0: ClientConnected?.Invoke(e.ConnId, e.Payload); break;
                case 1: MessageReceived?.Invoke(e.ConnId, e.Payload); break;
                case 2: ClientDisconnected?.Invoke(e.ConnId); break;
            }
        }
    }

    public void Send(int connectionId, string message)
    {
        if (!clients.TryGetValue(connectionId, out Client client) || client.Closed) return;

        byte[] frame = BuildTextFrame(Encoding.UTF8.GetBytes(message));
        EnqueueFrame(connectionId, client, frame);
    }

    public void Kick(int connectionId)
    {
        if (clients.TryGetValue(connectionId, out Client client))
            CloseClient(connectionId, client);
    }

    public void Stop()
    {
        running = false;
        try { listener?.Stop(); } catch { }
        foreach (var pair in clients)
            CloseClient(pair.Key, pair.Value);
    }

    private void EnqueueFrame(int connId, Client client, byte[] frame)
    {
        bool added;
        try { added = client.Outbox.TryAdd(frame); }
        catch (InvalidOperationException) { return; }

        if (!added)
            CloseClient(connId, client);
    }

    private void AcceptLoop()
    {
        while (running)
        {
            TcpClient tcp;
            try { tcp = listener.AcceptTcpClient(); }
            catch { break; }

            int connId = Interlocked.Increment(ref nextId);
            var client = new Client { Tcp = tcp, Stream = tcp.GetStream() };
            clients[connId] = client;

            new Thread(() => WriterLoop(connId, client)) { IsBackground = true, Name = "ws-write-" + connId }.Start();
            new Thread(() => ClientLoop(connId, client)) { IsBackground = true, Name = "ws-read-" + connId }.Start();
        }
    }

    private void WriterLoop(int connId, Client client)
    {
        try
        {
            foreach (byte[] frame in client.Outbox.GetConsumingEnumerable())
                client.Stream.Write(frame, 0, frame.Length);
        }
        catch
        {
        }
        finally
        {
            CloseClient(connId, client);
        }
    }

    private void ClientLoop(int connId, Client client)
    {
        try
        {
            client.Tcp.ReceiveTimeout = 5000;
            string query = DoHandshake(client.Stream);
            client.Tcp.ReceiveTimeout = 0;

            events.Enqueue(new TransportEvent { Kind = 0, ConnId = connId, Payload = query });

            var messageBuffer = new MemoryStream();
            while (running && !client.Closed)
            {
                byte b0 = ReadByte(client.Stream);
                byte b1 = ReadByte(client.Stream);

                bool fin = (b0 & 0x80) != 0;
                int opcode = b0 & 0x0F;
                bool masked = (b1 & 0x80) != 0;
                long length = b1 & 0x7F;

                if (length == 126)
                {
                    byte[] ext = ReadExact(client.Stream, 2);
                    length = (ext[0] << 8) | ext[1];
                }
                else if (length == 127)
                {
                    byte[] ext = ReadExact(client.Stream, 8);
                    length = 0;
                    for (int i = 0; i < 8; i++) length = (length << 8) | ext[i];
                }

                if (length > MaxFrameBytes) throw new IOException("frame too large");

                byte[] mask = masked ? ReadExact(client.Stream, 4) : null;
                byte[] payload = ReadExact(client.Stream, (int)length);
                if (masked)
                    for (int i = 0; i < payload.Length; i++)
                        payload[i] ^= mask[i % 4];

                switch (opcode)
                {
                    case 0x1:
                    case 0x0:
                        messageBuffer.Write(payload, 0, payload.Length);
                        if (fin)
                        {
                            string text = Encoding.UTF8.GetString(messageBuffer.ToArray());
                            messageBuffer.SetLength(0);
                            events.Enqueue(new TransportEvent { Kind = 1, ConnId = connId, Payload = text });
                        }
                        break;
                    case 0x8:
                        EnqueueFrame(connId, client, BuildControlFrame(0x8, payload));
                        throw new IOException("client closed");
                    case 0x9:
                        EnqueueFrame(connId, client, BuildControlFrame(0xA, payload));
                        break;
                }
            }
        }
        catch
        {
        }
        finally
        {
            CloseClient(connId, client);
        }
    }

    private string DoHandshake(NetworkStream stream)
    {
        var headerBytes = new MemoryStream();
        int sequence = 0;
        while (sequence < 4)
        {
            int b = stream.ReadByte();
            if (b < 0) throw new IOException("handshake eof");
            headerBytes.WriteByte((byte)b);
            if (headerBytes.Length > 16 * 1024) throw new IOException("handshake too large");

            bool marker = (sequence % 2 == 0) ? b == '\r' : b == '\n';
            sequence = marker ? sequence + 1 : (b == '\r' ? 1 : 0);
        }

        string header = Encoding.ASCII.GetString(headerBytes.ToArray());
        string[] lines = header.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        string requestLine = lines[0];
        string query = "";
        int qIndex = requestLine.IndexOf('?');
        if (qIndex >= 0)
        {
            int end = requestLine.IndexOf(' ', qIndex);
            query = requestLine.Substring(qIndex + 1, end - qIndex - 1);
        }

        string key = null;
        foreach (string line in lines)
        {
            int colon = line.IndexOf(':');
            if (colon < 0) continue;
            if (line.Substring(0, colon).Trim().Equals("Sec-WebSocket-Key", StringComparison.OrdinalIgnoreCase))
                key = line.Substring(colon + 1).Trim();
        }
        if (key == null) throw new IOException("no websocket key");

        string accept;
        using (var sha1 = SHA1.Create())
            accept = Convert.ToBase64String(sha1.ComputeHash(Encoding.ASCII.GetBytes(key + WsGuid)));

        byte[] response = Encoding.ASCII.GetBytes(
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Upgrade: websocket\r\n" +
            "Connection: Upgrade\r\n" +
            "Sec-WebSocket-Accept: " + accept + "\r\n\r\n");
        stream.Write(response, 0, response.Length);
        return query;
    }

    private static byte[] BuildTextFrame(byte[] payload)
    {
        using var ms = new MemoryStream();
        ms.WriteByte(0x81);
        if (payload.Length < 126)
        {
            ms.WriteByte((byte)payload.Length);
        }
        else if (payload.Length <= ushort.MaxValue)
        {
            ms.WriteByte(126);
            ms.WriteByte((byte)(payload.Length >> 8));
            ms.WriteByte((byte)(payload.Length & 0xFF));
        }
        else
        {
            ms.WriteByte(127);
            for (int i = 7; i >= 0; i--)
                ms.WriteByte((byte)((long)payload.Length >> (8 * i) & 0xFF));
        }
        ms.Write(payload, 0, payload.Length);
        return ms.ToArray();
    }

    private static byte[] BuildControlFrame(int opcode, byte[] payload)
    {
        var frame = new byte[2 + payload.Length];
        frame[0] = (byte)(0x80 | opcode);
        frame[1] = (byte)payload.Length;
        Array.Copy(payload, 0, frame, 2, payload.Length);
        return frame;
    }

    private void CloseClient(int connId, Client client)
    {
        if (client.Closed) return;
        client.Closed = true;
        try { client.Outbox.CompleteAdding(); } catch { }
        try { client.Tcp.Close(); } catch { }
        if (clients.TryRemove(connId, out _))
            events.Enqueue(new TransportEvent { Kind = 2, ConnId = connId });
    }

    private static byte ReadByte(NetworkStream stream)
    {
        int b = stream.ReadByte();
        if (b < 0) throw new IOException("eof");
        return (byte)b;
    }

    private static byte[] ReadExact(NetworkStream stream, int count)
    {
        var buffer = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            int read = stream.Read(buffer, offset, count - offset);
            if (read <= 0) throw new IOException("eof");
            offset += read;
        }
        return buffer;
    }
}
