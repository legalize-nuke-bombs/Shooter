using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Serialization;
using Shooter.Server.Characters;
using Shooter.Server.Session;
using Shooter.Client.Account;
using Shooter.Client.Characters;
using Shooter.Client.Chronology;
using Shooter.Logging;

namespace Shooter.Client
{
    public class NetworkClient : MonoBehaviour
    {
        public const float InputSendRate = 30f;

        public static NetworkClient Instance { get; private set; }

        public long PlayerId { get; private set; } = -1;
        public bool InWorld { get; private set; }

        public event Action<WorldJoinedMsg> WorldJoined;
        public event Action<SnapshotMsg> SnapshotReceived;
        public event Action<JoinedMsg> PlayerJoined;
        public event Action<LeftMsg> PlayerLeft;

        private ClientWebSocket socket;
        private CancellationTokenSource cancellation;
        private readonly ConcurrentQueue<string> inbound = new ConcurrentQueue<string>();
        private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded += (scene, _) => TrySpawn(scene.name);
            TrySpawn(SceneManager.GetActiveScene().name);
        }

        private static void TrySpawn(string sceneName)
        {
            if (sceneName != "Game") return;
            if (string.IsNullOrEmpty(Session.Token)) return;
            if (Instance != null) return;

            var go = new GameObject("Net");
            go.AddComponent<NetworkClient>();
            go.AddComponent<PlayersView>();
            go.AddComponent<ClockView>();
        }

        private void Awake()
        {
            Instance = this;
            Application.runInBackground = true;
            _ = Connect();
        }

        public void SendInput(InputMsg input)
        {
            _ = Send(input);
        }

        private async Task Connect()
        {
            socket = new ClientWebSocket();
            cancellation = new CancellationTokenSource();
            try
            {
                await socket.ConnectAsync(new Uri(Session.WsUrl), cancellation.Token).ConfigureAwait(false);
                Log.Info("net: connected " + Session.WsUrl);
                _ = ReceiveLoop();
                await Send(new HelloMsg { name = Session.DisplayName }).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log.Warn("net: connect failed: " + e.Message);
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[16 * 1024];
            var message = new StringBuilder();
            try
            {
                while (socket.State == WebSocketState.Open && !cancellation.IsCancellationRequested)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellation.Token).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Log.Warn("net: closed by server");
                        break;
                    }

                    message.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    if (!result.EndOfMessage) continue;

                    inbound.Enqueue(message.ToString());
                    message.Clear();
                }
            }
            catch (Exception e)
            {
                if (!cancellation.IsCancellationRequested)
                    Log.Warn("net: receive loop ended: " + e.Message);
            }
        }

        private async Task Send(Serializable msg)
        {
            if (socket is not { State: WebSocketState.Open }) return;

            byte[] bytes = Encoding.UTF8.GetBytes(Json.Serialize(msg));
            await sendLock.WaitAsync(cancellation.Token).ConfigureAwait(false);
            try
            {
                await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellation.Token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log.Warn("net: send failed: " + e.Message);
            }
            finally
            {
                sendLock.Release();
            }
        }

        private void Update()
        {
            while (inbound.TryDequeue(out string json))
                Dispatch(json);
        }

        private void Dispatch(string json)
        {
            switch (Json.Deserialize(json))
            {
                case WelcomeMsg welcome:
                    PlayerId = welcome.playerId;
                    Log.Info("net: welcome, playerId " + PlayerId + ", tickRate " + welcome.tickRate);
                    _ = Send(new JoinWorldMsg());
                    break;
                case WorldJoinedMsg worldJoined:
                    InWorld = true;
                    Log.Info("net: world " + worldJoined.worldId + ", players " + worldJoined.players.Length);
                    WorldJoined?.Invoke(worldJoined);
                    break;
                case SnapshotMsg snapshot:
                    SnapshotReceived?.Invoke(snapshot);
                    break;
                case JoinedMsg joined:
                    PlayerJoined?.Invoke(joined);
                    break;
                case LeftMsg left:
                    PlayerLeft?.Invoke(left);
                    break;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            cancellation?.Cancel();
            if (socket is { State: WebSocketState.Open })
                _ = socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
            socket?.Dispose();
        }
    }
}
