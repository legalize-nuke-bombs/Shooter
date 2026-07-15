using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkClient : MonoBehaviour
{
    private const string GameSceneName = "SampleScene";
    private const float InputSendRate = 30f;

    public static NetworkClient Instance { get; private set; }

    public long PlayerId { get; private set; } = -1;
    public bool InRoom { get; private set; }

    public event Action<RoomJoinedMsg> RoomJoined;
    public event Action<SnapshotMsg> SnapshotReceived;
    public event Action<JoinedMsg> PlayerJoined;
    public event Action<LeftMsg> PlayerLeft;

    private ClientWebSocket socket;
    private CancellationTokenSource cancellation;
    private readonly ConcurrentQueue<string> inbound = new ConcurrentQueue<string>();
    private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);

    private PlayerController localPlayer;
    private int inputSeq;
    private float nextInputTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded += (scene, _) => TrySpawn(scene.name);
        TrySpawn(SceneManager.GetActiveScene().name);
    }

    private static void TrySpawn(string sceneName)
    {
        if (sceneName != GameSceneName) return;
        if (string.IsNullOrEmpty(ConnectionConfig.Token)) return;
        if (Instance != null) return;

        var go = new GameObject("Net");
        go.AddComponent<NetworkClient>();
        go.AddComponent<RemotePlayerManager>();
    }

    private void Awake()
    {
        Instance = this;
        localPlayer = FindFirstObjectByType<PlayerController>();
        _ = Connect();
    }

    private async Task Connect()
    {
        socket = new ClientWebSocket();
        cancellation = new CancellationTokenSource();
        try
        {
            await socket.ConnectAsync(new Uri(ConnectionConfig.WsUrl), cancellation.Token);
            Debug.Log("net: connected " + ConnectionConfig.WsUrl);
            _ = ReceiveLoop();
            await Send(new HelloMsg { name = ConnectionConfig.DisplayName });
        }
        catch (Exception e)
        {
            Debug.LogWarning("net: connect failed: " + e.Message);
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
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellation.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Debug.LogWarning("net: closed by server");
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
                Debug.LogWarning("net: receive loop ended: " + e.Message);
        }
    }

    private async Task Send(object msg)
    {
        if (socket is not { State: WebSocketState.Open }) return;

        byte[] bytes = Encoding.UTF8.GetBytes(NetJson.Serialize(msg));
        await sendLock.WaitAsync(cancellation.Token);
        try
        {
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellation.Token);
        }
        catch (Exception e)
        {
            Debug.LogWarning("net: send failed: " + e.Message);
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

        if (InRoom && localPlayer != null && Time.time >= nextInputTime)
        {
            nextInputTime = Time.time + 1f / InputSendRate;
            InputMsg input = localPlayer.BuildInputMessage();
            input.seq = ++inputSeq;
            _ = Send(input);
        }
    }

    private void Dispatch(string json)
    {
        switch (NetJson.PeekType(json))
        {
            case "welcome":
                var welcome = NetJson.Parse<WelcomeMsg>(json);
                PlayerId = welcome.playerId;
                Debug.Log("net: welcome, playerId " + PlayerId + ", tickRate " + welcome.tickRate);
                _ = Send(new JoinRoomMsg { code = ConnectionConfig.RoomCode });
                break;
            case "roomJoined":
                var joined = NetJson.Parse<RoomJoinedMsg>(json);
                InRoom = true;
                Debug.Log("net: room " + joined.roomId + ", players " + joined.players.Length);
                RoomJoined?.Invoke(joined);
                break;
            case "snapshot":
                SnapshotReceived?.Invoke(NetJson.Parse<SnapshotMsg>(json));
                break;
            case "joined":
                PlayerJoined?.Invoke(NetJson.Parse<JoinedMsg>(json));
                break;
            case "left":
                PlayerLeft?.Invoke(NetJson.Parse<LeftMsg>(json));
                break;
            default:
                Debug.LogWarning("net: unknown message: " + json);
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
