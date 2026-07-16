using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameServer : MonoBehaviour
{
    private const float TickRate = 30f;
    private const float WalkSpeed = 5f;
    private const float SprintSpeed = 8f;
    private const float JumpHeight = 1.2f;
    private const float Gravity = -20f;
    private const string DefaultWorldId = "default";
    private const float WorldSpacing = 1000f;

    private const float BanSweepInterval = 60f;
    private const long BanRetentionSeconds = 180;

    private INetTransport transport;
    private readonly Dictionary<int, ServerPlayer> players = new Dictionary<int, ServerPlayer>();
    private readonly Dictionary<string, int> worldIndices = new Dictionary<string, int>();
    private readonly Dictionary<string, long> pairBans = new Dictionary<string, long>();
    private readonly Dictionary<long, long> userBans = new Dictionary<long, long>();
    private readonly Dictionary<string, long> worldBans = new Dictionary<string, long>();
    private byte[] jwtSecret;
    private float tickTimer;
    private float banSweepTimer;
    private long tick;

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
        string secret = ParseStringArg("-jwtSecret", Environment.GetEnvironmentVariable("UNITY_SERVER_SECRET") ?? "");
        if (string.IsNullOrEmpty(secret))
        {
            Debug.LogError("server: no JWT secret (-jwtSecret arg or UNITY_SERVER_SECRET env), refusing to start");
            Application.Quit(1);
            return;
        }
        jwtSecret = Convert.FromBase64String(secret);

        transport = new WsTransport();
        transport.ClientConnected += OnClientConnected;
        transport.MessageReceived += OnMessageReceived;
        transport.ClientDisconnected += OnClientDisconnected;
        transport.HookReceived += OnHookReceived;
        transport.HookAuthorizer = token => JwtVerifier.TryVerify(token, jwtSecret, out JwtClaims claims) && claims.sub == "hook";
        transport.Start(port);
        Debug.Log("server: ws listening on " + port);
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
            BroadcastSnapshots();
        }

        banSweepTimer += Time.deltaTime;
        if (banSweepTimer >= BanSweepInterval)
        {
            banSweepTimer = 0f;
            SweepBans();
        }
    }

    private void OnClientConnected(int connId, string query)
    {
        string token = ExtractQueryParam(query, "token");
        if (!JwtVerifier.TryVerify(token, jwtSecret, out JwtClaims claims))
        {
            Debug.Log("server: conn " + connId + " with invalid token, kicking");
            transport.Kick(connId);
            return;
        }

        string[] subject = claims.sub.Split(new[] { ':' }, 2);
        if (!long.TryParse(subject[0], out long userId))
        {
            Debug.Log("server: conn " + connId + " with malformed subject, kicking");
            transport.Kick(connId);
            return;
        }

        string worldId = subject.Length > 1 && subject[1].Length > 0 ? subject[1] : DefaultWorldId;

        if (IsBanned(userId, worldId, claims.iat))
        {
            Debug.Log("server: conn " + connId + " user " + userId + " world " + worldId + " with pre-kick token, kicking");
            transport.Kick(connId);
            return;
        }

        var player = new ServerPlayer
        {
            ConnId = connId,
            UserId = userId,
            DisplayName = "player" + userId,
            WorldId = worldId,
            Authed = true
        };
        players[connId] = player;

        Debug.Log("server: conn " + connId + " authed as " + player.UserId + " (" + player.DisplayName + ") world " + player.WorldId);
        transport.Send(connId, NetJson.Serialize(new WelcomeMsg { type = "welcome", playerId = player.UserId, tickRate = (int)TickRate }));
    }

    private void OnMessageReceived(int connId, string json)
    {
        if (!players.TryGetValue(connId, out ServerPlayer player)) return;

        switch (NetJson.PeekType(json))
        {
            case "hello":
                var hello = NetJson.Parse<HelloMsg>(json);
                if (!string.IsNullOrEmpty(hello.name))
                    player.DisplayName = hello.name.Length > 40 ? hello.name.Substring(0, 40) : hello.name;
                break;
            case "joinRoom":
                if (player.Authed && !player.InRoom)
                    JoinWorld(player);
                break;
            case "input":
                var input = NetJson.Parse<InputMsg>(json);
                player.LastInput = input;
                if (input.jump) player.JumpQueued = true;
                break;
        }
    }

    private void JoinWorld(ServerPlayer player)
    {
        player.InRoom = true;
        SpawnBody(player);

        var states = new List<PlayerStateMsg>();
        foreach (ServerPlayer p in players.Values)
            if (p.InRoom && p.WorldId == player.WorldId)
                states.Add(BuildState(p));

        transport.Send(player.ConnId, NetJson.Serialize(new RoomJoinedMsg
        {
            type = "roomJoined",
            roomId = player.WorldId,
            players = states.ToArray()
        }));

        string joined = NetJson.Serialize(new JoinedMsg { type = "joined", id = player.UserId, name = player.DisplayName });
        foreach (ServerPlayer p in players.Values)
            if (p.InRoom && p.WorldId == player.WorldId && p.ConnId != player.ConnId)
                transport.Send(p.ConnId, joined);

        Debug.Log("server: player " + player.UserId + " joined world " + player.WorldId + ", players there " + states.Count);
    }

    private float WorldOffsetX(string worldId)
    {
        if (!worldIndices.TryGetValue(worldId, out int index))
        {
            index = worldIndices.Count;
            worldIndices[worldId] = index;
        }
        return index * WorldSpacing;
    }

    private void SpawnBody(ServerPlayer player)
    {
        var body = new GameObject("Sim_" + player.UserId);

        float angle = (player.ConnId * 137f) % 360f;
        Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 3f;
        body.transform.position = new Vector3(WorldOffsetX(player.WorldId) + offset.x, 1.1f, offset.z);

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

    private void BroadcastSnapshots()
    {
        var statesByWorld = new Dictionary<string, List<PlayerStateMsg>>();
        foreach (ServerPlayer p in players.Values)
        {
            if (!p.InRoom) continue;
            if (!statesByWorld.TryGetValue(p.WorldId, out List<PlayerStateMsg> list))
            {
                list = new List<PlayerStateMsg>();
                statesByWorld[p.WorldId] = list;
            }
            list.Add(BuildState(p));
        }

        var jsonByWorld = new Dictionary<string, string>();
        foreach (KeyValuePair<string, List<PlayerStateMsg>> pair in statesByWorld)
            jsonByWorld[pair.Key] = NetJson.Serialize(new SnapshotMsg
            {
                type = "snapshot",
                tick = tick,
                players = pair.Value.ToArray()
            });

        foreach (ServerPlayer p in players.Values)
            if (p.InRoom)
                transport.Send(p.ConnId, jsonByWorld[p.WorldId]);
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

    private void OnHookReceived(string json)
    {
        HookBatchMsg batch = NetJson.Parse<HookBatchMsg>(json);
        if (batch == null || batch.hooks == null) return;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        foreach (UnityHookMsg hook in batch.hooks)
            ApplyHook(hook, now);
    }

    private void ApplyHook(UnityHookMsg hook, long now)
    {
        bool hasUser = hook.userIdToKick != 0;
        bool hasWorld = !string.IsNullOrEmpty(hook.worldIdToKick);

        if (hasUser && hasWorld) pairBans[hook.userIdToKick + ":" + hook.worldIdToKick] = now;
        else if (hasUser) userBans[hook.userIdToKick] = now;
        else if (hasWorld) worldBans[hook.worldIdToKick] = now;
        else return;

        var toKick = new List<int>();
        foreach (ServerPlayer p in players.Values)
        {
            bool userMatch = !hasUser || p.UserId == hook.userIdToKick;
            bool worldMatch = !hasWorld || p.WorldId == hook.worldIdToKick;
            if (userMatch && worldMatch)
                toKick.Add(p.ConnId);
        }
        foreach (int connId in toKick)
            transport.Kick(connId);

        Debug.Log("server: hook user " + hook.userIdToKick + " world " + hook.worldIdToKick + " kicked " + toKick.Count);
    }

    private bool IsBanned(long userId, string worldId, long tokenIat)
    {
        if (pairBans.TryGetValue(userId + ":" + worldId, out long banTime) && tokenIat < banTime) return true;
        if (userBans.TryGetValue(userId, out banTime) && tokenIat < banTime) return true;
        if (worldBans.TryGetValue(worldId, out banTime) && tokenIat < banTime) return true;
        return false;
    }

    private void SweepBans()
    {
        long cutoff = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - BanRetentionSeconds;
        SweepBanMap(pairBans, cutoff);
        SweepBanMap(userBans, cutoff);
        SweepBanMap(worldBans, cutoff);
    }

    private static void SweepBanMap<TKey>(Dictionary<TKey, long> bans, long cutoff)
    {
        var expired = new List<TKey>();
        foreach (KeyValuePair<TKey, long> pair in bans)
            if (pair.Value < cutoff)
                expired.Add(pair.Key);
        foreach (TKey key in expired)
            bans.Remove(key);
    }

    private void OnClientDisconnected(int connId)
    {
        if (!players.TryGetValue(connId, out ServerPlayer player)) return;
        players.Remove(connId);

        if (player.Body != null) Destroy(player.Body);
        if (!player.InRoom) return;

        string left = NetJson.Serialize(new LeftMsg { type = "left", id = player.UserId });
        foreach (ServerPlayer p in players.Values)
            if (p.InRoom && p.WorldId == player.WorldId)
                transport.Send(p.ConnId, left);

        Debug.Log("server: player " + player.UserId + " left world " + player.WorldId);
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
