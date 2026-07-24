using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Shooter.Logging;
using Shooter.Server.Sessions;

namespace Shooter.Server.Transport
{
    public class ServerWsTransport : IServerTransport
    {
        private const int MaxBodyBytes = 1024 * 1024;
        private const int OutboxCapacity = 256;
        private const int HandshakeTimeout = 5000;
        private const string HookPath = "/hooks";

        public event Action<int, string> ClientConnected;
        public event Action<int, string> MessageReceived;
        public event Action<int> ClientDisconnected;
        public event Action<string> HookReceived;

        private readonly HookAuthority hookAuthority;
        private readonly ConcurrentDictionary<int, Client> clients = new ConcurrentDictionary<int, Client>();
        private readonly ConcurrentQueue<TransportEvent> events = new ConcurrentQueue<TransportEvent>();

        private TcpListener listener;
        private Thread acceptThread;
        private volatile bool running;
        private int nextId;

        public ServerWsTransport(HookAuthority hookAuthority)
        {
            this.hookAuthority = hookAuthority;
        }

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

            EnqueueFrame(connectionId, client, WsFrames.Text(Encoding.UTF8.GetBytes(message)));
        }

        public void Kick(int connectionId)
        {
            if (clients.TryGetValue(connectionId, out Client client))
                CloseClient(connectionId, client);
        }

        public void Stop()
        {
            running = false;

            try
            {
                listener?.Stop();
            }
            catch (SocketException e)
            {
                Log.Warn("Listener stop failed: {}", e.Message);
            }

            foreach (var pair in clients)
                CloseClient(pair.Key, pair.Value);

            Log.Info("WS transport stopped");
        }

        private void EnqueueFrame(int connId, Client client, byte[] frame)
        {
            bool added;
            try
            {
                added = client.Outbox.TryAdd(frame);
            }
            catch (InvalidOperationException)
            {
                return;
            }

            if (added) return;

            Log.Warn("Conn {} outbox is full, closing", connId);
            CloseClient(connId, client);
        }

        private void AcceptLoop()
        {
            while (running)
            {
                TcpClient tcp;
                try
                {
                    tcp = listener.AcceptTcpClient();
                }
                catch (Exception e)
                {
                    if (running) Log.Error("Accept loop stopped, server takes no new connections: {}", e.Message);
                    break;
                }

                int connId = Interlocked.Increment(ref nextId);
                var client = new Client { Tcp = tcp, Stream = tcp.GetStream() };
                clients[connId] = client;
                Log.Info("TCP conn {} accepted from {}", connId, tcp.Client.RemoteEndPoint);

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
            catch (Exception e)
            {
                Log.Info("Conn {} writer stopped: {}", connId, e.Message);
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
                client.Tcp.ReceiveTimeout = HandshakeTimeout;
                HttpHead head = HttpHead.Read(client.Stream);

                if (head.Method == "POST" && head.Path == HookPath)
                {
                    closeReason = "hook request served";
                    ServeHook(client, head);
                    return;
                }

                string query = WsFrames.CompleteHandshake(client.Stream, head);
                client.Tcp.ReceiveTimeout = 0;
                Log.Info("Conn {} ws handshake ok, path {}", connId, head.Path);

                events.Enqueue(new TransportEvent { Kind = EventKind.Connected, ConnId = connId, Payload = query });

                ReadMessages(connId, client);
            }
            catch (Exception e)
            {
                closeReason = e.Message;
            }
            finally
            {
                Log.Info("Conn {} closed: {}", connId, closeReason);
                CloseClient(connId, client);
            }
        }

        private void ReadMessages(int connId, Client client)
        {
            var messageBuffer = new MemoryStream();

            while (running && !client.Closed)
            {
                WsFrame frame = WsFrames.Read(client.Stream);

                switch (frame.Opcode)
                {
                    case WsFrames.TextOpcode:
                    case WsFrames.ContinuationOpcode:
                        messageBuffer.Write(frame.Payload, 0, frame.Payload.Length);
                        if (!frame.Final) break;

                        string text = Encoding.UTF8.GetString(messageBuffer.ToArray());
                        messageBuffer.SetLength(0);
                        events.Enqueue(new TransportEvent { Kind = EventKind.Message, ConnId = connId, Payload = text });
                        break;
                    case WsFrames.CloseOpcode:
                        EnqueueFrame(connId, client, WsFrames.Control(WsFrames.CloseOpcode, frame.Payload));
                        throw new IOException("client closed");
                    case WsFrames.PingOpcode:
                        EnqueueFrame(connId, client, WsFrames.Control(WsFrames.PongOpcode, frame.Payload));
                        break;
                }
            }
        }

        private void ServeHook(Client client, HttpHead head)
        {
            string auth = head.Header("Authorization") ?? "";
            string token = auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? auth.Substring(7).Trim() : null;

            if (token == null || !hookAuthority.Allows(token))
            {
                Log.Warn("Hook post rejected: bad or missing bearer token");
                HttpHead.WriteResponse(client.Stream, "401 Unauthorized");
                return;
            }

            if (!int.TryParse(head.Header("Content-Length"), out int length) || length < 0 || length > MaxBodyBytes)
            {
                Log.Warn("Hook post rejected: bad content length");
                HttpHead.WriteResponse(client.Stream, "411 Length Required");
                return;
            }

            string body = Encoding.UTF8.GetString(WsFrames.ReadExact(client.Stream, length));
            events.Enqueue(new TransportEvent { Kind = EventKind.Hook, ConnId = 0, Payload = body });
            Log.Info("Hook post accepted, {} bytes", length);
            HttpHead.WriteResponse(client.Stream, "200 OK", "{\"accepted\":true}");
        }

        private void CloseClient(int connId, Client client)
        {
            if (client.Closed) return;

            client.Closed = true;

            try
            {
                client.Outbox.CompleteAdding();
            }
            catch (ObjectDisposedException)
            {
            }

            try
            {
                client.Tcp.Close();
            }
            catch (SocketException e)
            {
                Log.Warn("Conn {} socket close failed: {}", connId, e.Message);
            }

            if (clients.TryRemove(connId, out _))
                events.Enqueue(new TransportEvent { Kind = EventKind.Disconnected, ConnId = connId });
        }
    }
}
