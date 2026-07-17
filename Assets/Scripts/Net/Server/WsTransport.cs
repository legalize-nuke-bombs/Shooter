using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Shooter.Net
{
    public class WsTransport : INetTransport
    {
        private const string WsGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private const int MaxFrameBytes = 1024 * 1024;
        private const int OutboxCapacity = 256;

        public event Action<int, string> ClientConnected;
        public event Action<int, string> MessageReceived;
        public event Action<int> ClientDisconnected;
        public event Action<string> HookReceived;

        public Func<string, bool> HookAuthorizer { get; set; }

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

        private enum EventKind
        {
            Connected,
            Message,
            Disconnected,
            Hook
        }

        private struct TransportEvent
        {
            public EventKind Kind;
            public int ConnId;
            public string Payload;
        }

        private class HttpRequest
        {
            public string Method;
            public string Path;
            public string Query;
            public readonly Dictionary<string, string> Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public string Header(string name)
            {
                return Headers.TryGetValue(name, out string value) ? value : null;
            }
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
                    case EventKind.Connected: ClientConnected?.Invoke(e.ConnId, e.Payload); break;
                    case EventKind.Message: MessageReceived?.Invoke(e.ConnId, e.Payload); break;
                    case EventKind.Disconnected: ClientDisconnected?.Invoke(e.ConnId); break;
                    case EventKind.Hook: HookReceived?.Invoke(e.Payload); break;
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
                ServerLog.Info("tcp conn " + connId + " accepted from " + tcp.Client.RemoteEndPoint);

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
            string closeReason = "reader done";
            try
            {
                client.Tcp.ReceiveTimeout = 5000;
                HttpRequest request = ReadHttpRequest(client.Stream);

                if (request.Method == "POST" && request.Path == "/hooks")
                {
                    closeReason = "hook request served";
                    HandleHookRequest(client, request);
                    return;
                }

                string query = CompleteWsHandshake(client.Stream, request);
                client.Tcp.ReceiveTimeout = 0;
                ServerLog.Info("conn " + connId + " ws handshake ok, path " + request.Path);

                events.Enqueue(new TransportEvent { Kind = EventKind.Connected, ConnId = connId, Payload = query });

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
                                events.Enqueue(new TransportEvent { Kind = EventKind.Message, ConnId = connId, Payload = text });
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
            catch (Exception e)
            {
                closeReason = e.Message;
            }
            finally
            {
                ServerLog.Info("conn " + connId + " closed: " + closeReason);
                CloseClient(connId, client);
            }
        }

        private static HttpRequest ReadHttpRequest(NetworkStream stream)
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

            string[] requestParts = lines[0].Split(' ');
            if (requestParts.Length < 2) throw new IOException("malformed request line");

            var request = new HttpRequest { Method = requestParts[0] };
            string target = requestParts[1];
            int qIndex = target.IndexOf('?');
            request.Path = qIndex >= 0 ? target.Substring(0, qIndex) : target;
            request.Query = qIndex >= 0 ? target.Substring(qIndex + 1) : "";

            for (int i = 1; i < lines.Length; i++)
            {
                int colon = lines[i].IndexOf(':');
                if (colon < 0) continue;
                request.Headers[lines[i].Substring(0, colon).Trim()] = lines[i].Substring(colon + 1).Trim();
            }

            return request;
        }

        private static string CompleteWsHandshake(NetworkStream stream, HttpRequest request)
        {
            string key = request.Header("Sec-WebSocket-Key");
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
            return request.Query;
        }

        private void HandleHookRequest(Client client, HttpRequest request)
        {
            string auth = request.Header("Authorization") ?? "";
            string token = auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? auth.Substring(7).Trim() : null;
            Func<string, bool> authorizer = HookAuthorizer;

            if (token == null || authorizer == null || !authorizer(token))
            {
                ServerLog.Warn("hook post rejected: bad or missing bearer token");
                WriteHttpResponse(client.Stream, "401 Unauthorized");
                return;
            }

            if (!int.TryParse(request.Header("Content-Length"), out int length) || length < 0 || length > MaxFrameBytes)
            {
                ServerLog.Warn("hook post rejected: bad content length");
                WriteHttpResponse(client.Stream, "411 Length Required");
                return;
            }

            string body = Encoding.UTF8.GetString(ReadExact(client.Stream, length));
            events.Enqueue(new TransportEvent { Kind = EventKind.Hook, ConnId = 0, Payload = body });
            ServerLog.Info("hook post accepted, " + length + " bytes");
            WriteHttpResponse(client.Stream, "200 OK", "{\"accepted\":true}");
        }

        private static void WriteHttpResponse(NetworkStream stream, string status, string body = null)
        {
            byte[] payload = body == null ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(body);
            byte[] response = Encoding.ASCII.GetBytes(
                "HTTP/1.1 " + status + "\r\n" +
                (body == null ? "" : "Content-Type: application/json\r\n") +
                "Content-Length: " + payload.Length + "\r\n" +
                "Connection: close\r\n\r\n");
            stream.Write(response, 0, response.Length);
            if (payload.Length > 0)
                stream.Write(payload, 0, payload.Length);
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
                events.Enqueue(new TransportEvent { Kind = EventKind.Disconnected, ConnId = connId });
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
}
