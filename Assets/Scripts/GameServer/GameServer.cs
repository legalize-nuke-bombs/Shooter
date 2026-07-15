using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class GameServer : MonoBehaviour
{
    private const float TickRate = 30f;
    private const float WalkSpeed = 5f;
    private const float SprintSpeed = 8f;
    private const float JumpHeight = 1.2f;
    private const float Gravity = -20f;
    private const string RoomId = "default";

    private INetTransport transport;
    private readonly Dictionary<int, ServerPlayer> players = new Dictionary<int, ServerPlayer>();
    private string springBase;
    private float tickTimer;
    private long tick;

    [Serializable]
    private class UserMeResponse
    {
        public long id;
        public string displayName;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        Application.runInBackground = true;
        Application.targetFrameRate = (int)TickRate * 2;

        int port = ParseIntArg("-port", 9090);
        springBase = ParseStringArg("-spring", "http://localhost:8080");

        transport = new WsTransport();
        transport.ClientConnected += OnClientConnected;
        transport.MessageReceived += OnMessageReceived;
        transport.ClientDisconnected += OnClientDisconnected;
        transport.Start(port);
        Debug.Log("server: ws listening on " + port + ", spring at " + springBase);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayerController localPlayer = FindAnyObjectByType<PlayerController>();
        if (localPlayer != null)
            Destroy(localPlayer.gameObject);
        Debug.Log("server: scene " + scene.name + " ready");
    }

    private void Update()
    {
        transport.Poll();

        tickTimer += Time.deltaTime;
        float tickInterval = 1f / TickRate;
        while (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;
            Simulate(tickInterval);
            tick++;
            BroadcastSnapshot();
        }
    }

    private void OnClientConnected(int connId, string query)
    {
        var player = new ServerPlayer { ConnId = connId };
        players[connId] = player;

        string token = ExtractQueryParam(query, "token");
        if (string.IsNullOrEmpty(token))
        {
            Debug.Log("server: conn " + connId + " without token, kicking");
            transport.Kick(connId);
            return;
        }

        StartCoroutine(Authenticate(connId, token));
    }

    private IEnumerator Authenticate(int connId, string token)
    {
        using var request = UnityWebRequest.Get(springBase + "/api/users/me");
        request.SetRequestHeader("Authorization", "Bearer " + token);
        request.timeout = 5;
        yield return request.SendWebRequest();

        if (!players.TryGetValue(connId, out ServerPlayer player)) yield break;

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("server: auth failed for conn " + connId + ": " + request.responseCode);
            transport.Kick(connId);
            yield break;
        }

        var me = JsonUtility.FromJson<UserMeResponse>(request.downloadHandler.text);
        player.UserId = me.id;
        player.DisplayName = me.displayName;
        player.Authed = true;

        Debug.Log("server: conn " + connId + " authed as " + me.id + " (" + me.displayName + ")");
        transport.Send(connId, NetJson.Serialize(new WelcomeMsg { type = "welcome", playerId = me.id, tickRate = (int)TickRate }));
    }

    private void OnMessageReceived(int connId, string json)
    {
        if (!players.TryGetValue(connId, out ServerPlayer player)) return;

        switch (NetJson.PeekType(json))
        {
            case "hello":
                break;
            case "joinRoom":
                if (player.Authed && !player.InRoom)
                    JoinRoom(player);
                break;
            case "input":
                var input = NetJson.Parse<InputMsg>(json);
                player.LastInput = input;
                if (input.jump) player.JumpQueued = true;
                break;
        }
    }

    private void JoinRoom(ServerPlayer player)
    {
        player.InRoom = true;
        SpawnBody(player);

        var states = new List<PlayerStateMsg>();
        foreach (ServerPlayer p in players.Values)
            if (p.InRoom)
                states.Add(BuildState(p));

        transport.Send(player.ConnId, NetJson.Serialize(new RoomJoinedMsg
        {
            type = "roomJoined",
            roomId = RoomId,
            players = states.ToArray()
        }));

        string joined = NetJson.Serialize(new JoinedMsg { type = "joined", id = player.UserId, name = player.DisplayName });
        foreach (ServerPlayer p in players.Values)
            if (p.InRoom && p.ConnId != player.ConnId)
                transport.Send(p.ConnId, joined);

        Debug.Log("server: player " + player.UserId + " joined room, total " + states.Count);
    }

    private void SpawnBody(ServerPlayer player)
    {
        var body = new GameObject("Sim_" + player.UserId);

        float angle = (player.ConnId * 137f) % 360f;
        Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 3f;
        body.transform.position = new Vector3(offset.x, 1.1f, offset.z);

        player.Body = body;
        player.Controller = body.AddComponent<CharacterController>();
    }

    private void Simulate(float dt)
    {
        foreach (ServerPlayer p in players.Values)
        {
            if (!p.InRoom || p.Controller == null) continue;

            InputMsg input = p.LastInput;
            p.Body.transform.rotation = Quaternion.Euler(0f, input.yaw, 0f);

            Vector3 direction = Vector3.ClampMagnitude(
                p.Body.transform.right * input.moveX + p.Body.transform.forward * input.moveZ, 1f);
            float speed = input.sprint ? SprintSpeed : WalkSpeed;

            if (p.Controller.isGrounded)
            {
                p.VerticalVelocity = -2f;
                if (p.JumpQueued)
                    p.VerticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
            }
            p.JumpQueued = false;

            p.VerticalVelocity += Gravity * dt;
            p.Controller.Move((direction * speed + Vector3.up * p.VerticalVelocity) * dt);
        }
    }

    private void BroadcastSnapshot()
    {
        var states = new List<PlayerStateMsg>();
        foreach (ServerPlayer p in players.Values)
            if (p.InRoom)
                states.Add(BuildState(p));
        if (states.Count == 0) return;

        string snapshot = NetJson.Serialize(new SnapshotMsg
        {
            type = "snapshot",
            tick = tick,
            players = states.ToArray()
        });

        foreach (ServerPlayer p in players.Values)
            if (p.InRoom)
                transport.Send(p.ConnId, snapshot);
    }

    private PlayerStateMsg BuildState(ServerPlayer p)
    {
        Vector3 pos = p.Body.transform.position;
        return new PlayerStateMsg
        {
            id = p.UserId,
            name = p.DisplayName,
            x = pos.x,
            y = pos.y,
            z = pos.z,
            yaw = p.Body.transform.eulerAngles.y,
            pitch = p.LastInput.pitch
        };
    }

    private void OnClientDisconnected(int connId)
    {
        if (!players.TryGetValue(connId, out ServerPlayer player)) return;
        players.Remove(connId);

        if (player.Body != null) Destroy(player.Body);
        if (!player.InRoom) return;

        string left = NetJson.Serialize(new LeftMsg { type = "left", id = player.UserId });
        foreach (ServerPlayer p in players.Values)
            if (p.InRoom)
                transport.Send(p.ConnId, left);

        Debug.Log("server: player " + player.UserId + " left");
    }

    private void OnDestroy()
    {
        transport?.Stop();
    }

    private static string ExtractQueryParam(string query, string name)
    {
        foreach (string pair in query.Split('&'))
        {
            int eq = pair.IndexOf('=');
            if (eq <= 0) continue;
            if (pair.Substring(0, eq) == name)
                return Uri.UnescapeDataString(pair.Substring(eq + 1));
        }
        return "";
    }

    private static int ParseIntArg(string name, int fallback)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name && int.TryParse(args[i + 1], out int value))
                return value;
        return fallback;
    }

    private static string ParseStringArg(string name, string fallback)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name)
                return args[i + 1];
        return fallback;
    }
}
