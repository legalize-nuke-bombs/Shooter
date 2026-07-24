using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shooter.Logging;

namespace Shooter.Client.Transport
{
    public class ClientWsTransport : IClientTransport
    {
        private const int ReceiveBufferBytes = 16 * 1024;

        public event Action<string> MessageReceived;

        private ClientWebSocket socket;
        private CancellationTokenSource cancellation;
        private readonly ConcurrentQueue<TransportEvent> events = new ConcurrentQueue<TransportEvent>();
        private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);

        private enum EventKind
        {
            Message
        }

        private struct TransportEvent
        {
            public EventKind Kind;
            public string Payload;
        }

        public void Connect(string url)
        {
            socket = new ClientWebSocket();
            cancellation = new CancellationTokenSource();
            _ = ConnectRoutine(url);
        }

        public void Poll()
        {
            while (events.TryDequeue(out TransportEvent e))
            {
                switch (e.Kind)
                {
                    case EventKind.Message: MessageReceived?.Invoke(e.Payload); break;
                }
            }
        }

        public void Send(string message)
        {
            _ = SendRoutine(message);
        }

        public void Stop()
        {
            cancellation?.Cancel();

            ClientWebSocket closing = socket;
            socket = null;
            if (closing != null) _ = CloseRoutine(closing);

            cancellation?.Dispose();
            cancellation = null;
            Log.Info("Transport stopped");
        }

        private static async Task CloseRoutine(ClientWebSocket closing)
        {
            try
            {
                if (closing.State == WebSocketState.Open)
                    await closing.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log.Info("Socket close failed: {}", e.Message);
            }
            finally
            {
                closing.Dispose();
            }
        }

        private async Task ConnectRoutine(string url)
        {
            try
            {
                await socket.ConnectAsync(new Uri(url), cancellation.Token).ConfigureAwait(false);
                Log.Info("connected {}", url);
                _ = ReceiveLoop();
            }
            catch (Exception e)
            {
                Log.Warn("connect failed: {}", e.Message);
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[ReceiveBufferBytes];
            var message = new StringBuilder();
            try
            {
                while (socket.State == WebSocketState.Open && !cancellation.IsCancellationRequested)
                {
                    WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellation.Token).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Log.Warn("closed by server");
                        break;
                    }

                    message.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    if (!result.EndOfMessage) continue;

                    events.Enqueue(new TransportEvent { Kind = EventKind.Message, Payload = message.ToString() });
                    message.Clear();
                }
            }
            catch (Exception e)
            {
                if (!cancellation.IsCancellationRequested)
                    Log.Warn("receive loop ended: {}", e.Message);
            }
        }

        private async Task SendRoutine(string message)
        {
            if (socket is not { State: WebSocketState.Open }) return;

            byte[] bytes = Encoding.UTF8.GetBytes(message);
            await sendLock.WaitAsync(cancellation.Token).ConfigureAwait(false);
            try
            {
                await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellation.Token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log.Warn("send failed: {}", e.Message);
            }
            finally
            {
                sendLock.Release();
            }
        }
    }
}
