using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MenuController : MonoBehaviour
{
    private const string GameSceneName = "SampleScene";
    private const int PageSize = 20;

    private static readonly string[] SortLabels = { "Last active", "Newest", "Name" };
    private static readonly string[] SortOrders = { "ACCESSED_AT", "CREATED_AT", "NAME" };

    private VisualElement root;
    private VisualElement loginScreen;
    private VisualElement worldsScreen;
    private VisualElement createModal;
    private VisualElement displayNameBlock;
    private TextField serverField;
    private TextField usernameField;
    private TextField passwordField;
    private TextField displayNameField;
    private TextField worldIdField;
    private TextField createNameField;
    private TextField createDescField;
    private Toggle visPublic;
    private Toggle visPrivate;
    private DropdownField createPolicy;
    private DropdownField sortDropdown;
    private ScrollView worldsScroll;
    private Button submitBtn;
    private Button modeBtn;
    private Button tabPublic;
    private Button tabMine;
    private Label loginStatus;
    private Label worldsStatus;
    private Label createStatus;
    private Label serverInfo;
    private Label userLabel;

    private bool registerMode;
    private bool mineTab;
    private bool busy;
    private int page;
    private long myUserId = -1;

    [Serializable] private class TokenResponse { public string token; }
    [Serializable] private class ServerInfoResponse { public string name; public int major; public int minor; public int patch; }
    [Serializable] private class UserDto { public long id; public string displayName; }
    [Serializable] private class PlayerDto { public long id; public UserDto user; public string role; public long memberSince; }
    [Serializable] private class WorldDto { public string id; public string name; public string description; public long createdAt; public string visibilityPolicy; public string joinPolicy; public PlayerDto[] players; }
    [Serializable] private class WorldsWrap { public WorldDto[] items; }
    [Serializable] private class LoginRequest { public string username; public string password; }
    [Serializable] private class RegisterRequest { public string username; public string displayName; public string password; }
    [Serializable] private class CreateWorldRequest { public string name; public string description; public string visibilityPolicy; public string joinPolicy; }

    private void Start()
    {
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        root = GetComponent<UIDocument>().rootVisualElement;
        loginScreen = root.Q<VisualElement>("login-screen");
        worldsScreen = root.Q<VisualElement>("worlds-screen");
        createModal = root.Q<VisualElement>("create-modal");
        displayNameBlock = root.Q<VisualElement>("displayname-block");
        serverField = root.Q<TextField>("server-field");
        usernameField = root.Q<TextField>("username-field");
        passwordField = root.Q<TextField>("password-field");
        displayNameField = root.Q<TextField>("displayname-field");
        worldIdField = root.Q<TextField>("world-id-field");
        createNameField = root.Q<TextField>("create-name");
        createDescField = root.Q<TextField>("create-desc");
        visPublic = root.Q<Toggle>("vis-public");
        visPrivate = root.Q<Toggle>("vis-private");
        createPolicy = root.Q<DropdownField>("create-policy");
        sortDropdown = root.Q<DropdownField>("sort-dropdown");
        worldsScroll = root.Q<ScrollView>("worlds-scroll");
        submitBtn = root.Q<Button>("submit-btn");
        modeBtn = root.Q<Button>("mode-btn");
        tabPublic = root.Q<Button>("tab-public");
        tabMine = root.Q<Button>("tab-mine");
        loginStatus = root.Q<Label>("login-status");
        worldsStatus = root.Q<Label>("worlds-status");
        createStatus = root.Q<Label>("create-status");
        serverInfo = root.Q<Label>("server-info");
        userLabel = root.Q<Label>("user-label");

        serverField.value = PlayerPrefs.GetString("serverAddress", "localhost:8080");
        usernameField.value = PlayerPrefs.GetString("username", "");
        displayNameBlock.AddToClassList("hidden");

        sortDropdown.choices = new List<string>(SortLabels);
        sortDropdown.index = 0;
        sortDropdown.RegisterValueChangedCallback(_ => ReloadWorlds());

        createPolicy.choices = new List<string> { "EVERYONE" };
        createPolicy.index = 0;
        createPolicy.SetEnabled(false);

        visPublic.RegisterValueChangedCallback(e => { if (e.newValue) visPrivate.SetValueWithoutNotify(false); else if (!visPrivate.value) visPublic.SetValueWithoutNotify(true); });
        visPrivate.RegisterValueChangedCallback(e => { if (e.newValue) visPublic.SetValueWithoutNotify(false); else if (!visPublic.value) visPrivate.SetValueWithoutNotify(true); });

        submitBtn.clicked += Submit;
        modeBtn.clicked += ToggleMode;
        passwordField.RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Return) Submit(); });
        root.Q<Button>("quit-btn").clicked += Application.Quit;
        root.Q<Button>("refresh-btn").clicked += ReloadWorlds;
        root.Q<Button>("load-more-btn").clicked += () => { page++; StartCoroutine(LoadWorlds(false)); };
        root.Q<Button>("join-id-btn").clicked += () => { string id = worldIdField.value.Trim(); if (id.Length > 0) StartCoroutine(Join(id)); };
        root.Q<Button>("create-btn").clicked += () => { createStatus.text = ""; createModal.RemoveFromClassList("hidden"); };
        root.Q<Button>("create-cancel").clicked += () => createModal.AddToClassList("hidden");
        root.Q<Button>("create-confirm").clicked += () => StartCoroutine(CreateWorld());
        tabPublic.clicked += () => SwitchTab(false);
        tabMine.clicked += () => SwitchTab(true);

        StartCoroutine(CheckServer());
    }

    private void ToggleMode()
    {
        registerMode = !registerMode;
        submitBtn.text = registerMode ? "REGISTER" : "LOGIN";
        modeBtn.text = registerMode ? "Have an account? Login" : "Need an account? Register";
        if (registerMode) displayNameBlock.RemoveFromClassList("hidden");
        else displayNameBlock.AddToClassList("hidden");
    }

    private void SwitchTab(bool mine)
    {
        mineTab = mine;
        if (mine) { tabMine.AddToClassList("tab-active"); tabPublic.RemoveFromClassList("tab-active"); }
        else { tabPublic.AddToClassList("tab-active"); tabMine.RemoveFromClassList("tab-active"); }
        ReloadWorlds();
    }

    private void ReloadWorlds()
    {
        page = 0;
        StartCoroutine(LoadWorlds(true));
    }

    private void Submit()
    {
        if (busy) return;
        loginStatus.text = "";

        string username = usernameField.value.Trim();
        string password = passwordField.value;
        string displayName = registerMode ? displayNameField.value.Trim() : username;

        if (!Regex.IsMatch(username, "^[a-zA-Z0-9_]{4,20}$")) { loginStatus.text = "username: 4-20 chars, letters/digits/underscore"; return; }
        if (password.Length < 8 || password.Length > 40) { loginStatus.text = "password: 8-40 chars"; return; }
        bool upper = false, lower = false, digit = false;
        foreach (char c in password)
        {
            if (char.IsUpper(c)) upper = true;
            else if (char.IsLower(c)) lower = true;
            else if (char.IsDigit(c)) digit = true;
        }
        if (!upper || !lower || !digit) { loginStatus.text = "password needs upper, lower and digit"; return; }
        if (registerMode && (displayName.Length < 1 || displayName.Length > 40)) { loginStatus.text = "display name: 1-40 chars"; return; }

        StartCoroutine(Authenticate(username, displayName, password));
    }

    private IEnumerator CheckServer()
    {
        serverInfo.text = "";
        yield return Request("GET", "/api/server", null, false, (code, text) =>
        {
            if (code == 200)
            {
                var info = JsonUtility.FromJson<ServerInfoResponse>(text);
                serverInfo.text = info.name + " · v" + info.major + "." + info.minor + "." + info.patch;
            }
            else serverInfo.text = "server unreachable";
        });
    }

    private IEnumerator Authenticate(string username, string displayName, string password)
    {
        busy = true;
        submitBtn.SetEnabled(false);
        loginStatus.text = registerMode ? "registering..." : "logging in...";

        string path = registerMode ? "/api/auth/register" : "/api/auth/login";
        string body = registerMode
            ? JsonUtility.ToJson(new RegisterRequest { username = username, displayName = displayName, password = password })
            : JsonUtility.ToJson(new LoginRequest { username = username, password = password });

        yield return Request("POST", path, body, false, (code, text) =>
        {
            busy = false;
            submitBtn.SetEnabled(true);

            if (code != 200 && code != 201) { loginStatus.text = "error " + code + ": " + text; return; }

            string token = JsonUtility.FromJson<TokenResponse>(text).token;
            if (string.IsNullOrEmpty(token)) { loginStatus.text = "no token in response"; return; }

            ConnectionConfig.Username = username;
            ConnectionConfig.DisplayName = displayName;
            ConnectionConfig.Token = token;
            ConnectionConfig.ServerAddress = serverField.value.Trim();
            myUserId = ExtractUserId(token);

            PlayerPrefs.SetString("username", username);
            PlayerPrefs.SetString("serverAddress", ConnectionConfig.ServerAddress);
            PlayerPrefs.Save();

            loginStatus.text = "";
            userLabel.text = displayName;
            loginScreen.AddToClassList("hidden");
            worldsScreen.RemoveFromClassList("hidden");
            ReloadWorlds();
        });
    }

    private static long ExtractUserId(string token)
    {
        try
        {
            string[] parts = token.Split('.');
            string payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4) { case 2: payload += "=="; break; case 3: payload += "="; break; }
            var claims = JsonUtility.FromJson<JwtClaims>(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            return long.Parse(claims.sub);
        }
        catch { return -1; }
    }

    private IEnumerator LoadWorlds(bool reset)
    {
        if (reset) worldsScroll.Clear();
        worldsStatus.text = "loading...";

        string path = "/api/worlds?order=" + SortOrders[Mathf.Max(0, sortDropdown.index)] + "&page=" + page + "&size=" + PageSize;
        if (mineTab) path += "&playerRole=CREATOR";

        yield return Request("GET", path, null, true, (code, text) =>
        {
            if (code != 200) { worldsStatus.text = "error " + code + ": " + text; return; }

            WorldDto[] worlds = JsonUtility.FromJson<WorldsWrap>("{\"items\":" + text + "}").items;
            foreach (WorldDto world in worlds)
                worldsScroll.Add(BuildCard(world));

            worldsStatus.text = worlds.Length == 0 ? (page == 0 ? "no worlds yet — create one" : "no more worlds") : "";
        });
    }

    private VisualElement BuildCard(WorldDto world)
    {
        var card = new VisualElement();
        card.AddToClassList("world-card");

        var info = new VisualElement();
        info.AddToClassList("world-info");

        var nameRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        var name = new Label(world.name);
        name.AddToClassList("world-name");
        nameRow.Add(name);

        string myRole = FindMyRole(world);
        if (myRole != null)
        {
            var badge = new Label(myRole);
            badge.AddToClassList("role-badge");
            badge.AddToClassList("role-" + myRole.ToLowerInvariant());
            nameRow.Add(badge);
        }
        if (world.visibilityPolicy == "PRIVATE")
        {
            var priv = new Label("PRIVATE");
            priv.AddToClassList("role-badge");
            priv.AddToClassList("role-member");
            nameRow.Add(priv);
        }
        info.Add(nameRow);

        if (!string.IsNullOrEmpty(world.description))
        {
            string desc = world.description.Length > 120 ? world.description.Substring(0, 120) + "…" : world.description;
            var descLabel = new Label(desc);
            descLabel.AddToClassList("world-desc");
            info.Add(descLabel);
        }

        var meta = new Label(BuildMeta(world));
        meta.AddToClassList("world-meta");
        info.Add(meta);
        card.Add(info);

        var joinBtn = new Button(() => StartCoroutine(Join(world.id))) { text = "JOIN" };
        joinBtn.AddToClassList("btn");
        joinBtn.AddToClassList("btn-primary");
        joinBtn.AddToClassList("join-btn");
        card.Add(joinBtn);

        return card;
    }

    private string FindMyRole(WorldDto world)
    {
        if (world.players == null || myUserId < 0) return null;
        foreach (PlayerDto p in world.players)
            if (p.user != null && p.user.id == myUserId)
                return p.role;
        return null;
    }

    private static string BuildMeta(WorldDto world)
    {
        int count = world.players?.Length ?? 0;
        string creator = "";
        if (world.players != null)
            foreach (PlayerDto p in world.players)
                if (p.role == "CREATOR" && p.user != null) { creator = " · by " + p.user.displayName; break; }

        long age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - world.createdAt;
        string ago = age < 3600 ? (age / 60) + "m ago" : age < 86400 ? (age / 3600) + "h ago" : (age / 86400) + "d ago";
        return count + (count == 1 ? " player" : " players") + " · " + ago + creator;
    }

    private IEnumerator Join(string worldId)
    {
        if (busy) yield break;
        busy = true;
        worldsStatus.text = "joining...";

        yield return Request("POST", "/api/worlds/" + worldId + "/join", "{}", true, (code, text) =>
        {
            busy = false;
            if (code != 200 && code != 201) { worldsStatus.text = "join failed " + code + ": " + text; return; }

            string worldToken = JsonUtility.FromJson<TokenResponse>(text).token;
            if (string.IsNullOrEmpty(worldToken)) { worldsStatus.text = "no token in response"; return; }

            ConnectionConfig.WorldToken = worldToken;
            ConnectionConfig.RoomCode = worldId;
            SceneManager.LoadScene(GameSceneName);
        });
    }

    private IEnumerator CreateWorld()
    {
        string name = createNameField.value.Trim();
        string desc = createDescField.value ?? "";
        if (name.Length < 1) { createStatus.text = "name required"; yield break; }

        createStatus.text = "creating...";
        string body = JsonUtility.ToJson(new CreateWorldRequest
        {
            name = name,
            description = desc,
            visibilityPolicy = visPrivate.value ? "PRIVATE" : "PUBLIC",
            joinPolicy = "EVERYONE"
        });

        yield return Request("POST", "/api/worlds", body, true, (code, text) =>
        {
            if (code != 200 && code != 201) { createStatus.text = "error " + code + ": " + text; return; }
            createModal.AddToClassList("hidden");
            createNameField.value = "";
            createDescField.value = "";
            ReloadWorlds();
        });
    }

    private IEnumerator Request(string method, string path, string body, bool auth, Action<long, string> onDone)
    {
        string url = "http://" + serverField.value.Trim() + path;
        using var request = new UnityWebRequest(url, method);
        if (body != null)
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            request.SetRequestHeader("Content-Type", "application/json");
        }
        request.downloadHandler = new DownloadHandlerBuffer();
        if (auth) request.SetRequestHeader("Authorization", "Bearer " + ConnectionConfig.Token);
        request.timeout = 10;

        yield return request.SendWebRequest();

        string text = request.downloadHandler.text ?? "";
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.DataProcessingError)
            onDone(0, request.error);
        else
            onDone(request.responseCode, text);
    }
}
