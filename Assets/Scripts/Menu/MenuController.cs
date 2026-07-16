using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

    private static readonly Dictionary<string, string> ErrorTexts = new Dictionary<string, string>
    {
        { "NOT_AUTHENTICATED", "Сессия протухла — войди заново" },
        { "INVALID_PASSWORD", "Неверный пароль" },
        { "WEAK_PASSWORD", "Пароль: 8-40 символов, заглавная, строчная и цифра" },
        { "USERNAME_TAKEN", "Имя пользователя занято — попробуй другое" },
        { "USER_NOT_FOUND", "Такого пользователя нет" },
        { "EMPTY_REQUEST", "Пустой запрос — попробуй ещё раз" },
        { "MALFORMED_REQUEST", "Запрос не понравился серверу — попробуй ещё раз" },
        { "WORLD_NOT_FOUND", "Мир не найден — проверь ID" },
        { "WORLD_DOES_NOT_ACCEPT_NEW_MEMBERS", "Этот мир не принимает новых игроков" },
        { "INTERNAL_ERROR", "Ошибка на сервере — попробуй ещё раз" }
    };

    private static readonly string[] VisibilityLabels = { "Скрытый", "Публичный" };
    private static readonly string[] VisibilityValues = { "PRIVATE", "PUBLIC" };
    private static readonly string[] PolicyLabels = { "Закрытый", "Открытый" };
    private static readonly string[] PolicyValues = { "NOBODY", "EVERYONE" };

    private VisualElement loginScreen;
    private VisualElement serverErrorScreen;
    private VisualElement worldsScreen;
    private VisualElement createModal;
    private VisualElement confirmBlock;
    private VisualElement displayNameBlock;
    private TextField usernameField;
    private TextField passwordField;
    private TextField confirmField;
    private TextField displayNameField;
    private TextField worldIdField;
    private TextField createNameField;
    private TextField createDescField;
    private DropdownField createVisibility;
    private DropdownField createPolicy;
    private ScrollView worldsScroll;
    private Button submitBtn;
    private Button modeLink;
    private Button tabPublic;
    private Button tabMine;
    private Button loadMoreBtn;
    private Label formTitle;
    private Label loginStatus;
    private Label worldsStatus;
    private Label createStatus;
    private Label cornerStatus;
    private Label serverErrorDetail;
    private Label userLabel;

    private string serverAddress = "localhost:8080";
    private bool registerMode = true;
    private bool mineTab = true;
    private bool busy;
    private bool serverOk;
    private int page;
    private long myUserId = -1;

    [Serializable] private class ConfigFile { public string serverAddress; }
    [Serializable] private class TokenResponse { public string token; }
    [Serializable] private class ProblemResponse { public string code; public string detail; }
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

        LoadConfig();

        var root = GetComponent<UIDocument>().rootVisualElement;
        var rootBox = root.Q<VisualElement>("root");
        rootBox.Insert(0, new MenuBackground());

        loginScreen = root.Q<VisualElement>("login-screen");
        serverErrorScreen = root.Q<VisualElement>("server-error-screen");
        worldsScreen = root.Q<VisualElement>("worlds-screen");
        createModal = root.Q<VisualElement>("create-modal");
        confirmBlock = root.Q<VisualElement>("confirm-block");
        displayNameBlock = root.Q<VisualElement>("displayname-block");
        usernameField = root.Q<TextField>("username-field");
        passwordField = root.Q<TextField>("password-field");
        confirmField = root.Q<TextField>("confirm-field");
        displayNameField = root.Q<TextField>("displayname-field");
        worldIdField = root.Q<TextField>("world-id-field");
        createNameField = root.Q<TextField>("create-name");
        createDescField = root.Q<TextField>("create-desc");
        createVisibility = root.Q<DropdownField>("create-visibility");
        createPolicy = root.Q<DropdownField>("create-policy");
        worldsScroll = root.Q<ScrollView>("worlds-scroll");
        submitBtn = root.Q<Button>("submit-btn");
        modeLink = root.Q<Button>("mode-link");
        tabPublic = root.Q<Button>("tab-public");
        tabMine = root.Q<Button>("tab-mine");
        loadMoreBtn = root.Q<Button>("load-more-btn");
        formTitle = root.Q<Label>("form-title");
        loginStatus = root.Q<Label>("login-status");
        worldsStatus = root.Q<Label>("worlds-status");
        createStatus = root.Q<Label>("create-status");
        cornerStatus = root.Q<Label>("corner-status");
        serverErrorDetail = root.Q<Label>("server-error-detail");
        userLabel = root.Q<Label>("user-label");

        usernameField.value = PlayerPrefs.GetString("username", "");

        createVisibility.choices = new List<string>(VisibilityLabels);
        createVisibility.index = 0;
        createPolicy.choices = new List<string>(PolicyLabels);
        createPolicy.index = 0;

        modeLink.clicked += () => SetRegisterMode(!registerMode);
        submitBtn.clicked += Submit;
        passwordField.RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Return && !registerMode) Submit(); });
        confirmField.RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Return) Submit(); });

        root.Q<Button>("retry-btn").clicked += () => StartCoroutine(CheckServer());
        root.Q<Button>("quit-btn").clicked += Application.Quit;
        root.Q<Button>("refresh-btn").clicked += ReloadWorlds;
        loadMoreBtn.clicked += () => { page++; StartCoroutine(LoadWorlds(false)); };
        root.Q<Button>("join-id-btn").clicked += () => { string id = worldIdField.value.Trim(); if (id.Length > 0) StartCoroutine(Join(id)); };
        root.Q<Button>("create-btn").clicked += () => { createStatus.text = ""; createModal.RemoveFromClassList("hidden"); };
        root.Q<Button>("create-cancel").clicked += () => createModal.AddToClassList("hidden");
        root.Q<Button>("create-confirm").clicked += () => StartCoroutine(CreateWorld());
        tabPublic.clicked += () => SwitchTab(false);
        tabMine.clicked += () => SwitchTab(true);

        if (usernameField.value.Length > 0)
            SetRegisterMode(false);

        StartCoroutine(CheckServer());
    }

    private void LoadConfig()
    {
        try
        {
            string path = Path.Combine(Application.streamingAssetsPath, "config.json");
            if (File.Exists(path))
            {
                var config = JsonUtility.FromJson<ConfigFile>(File.ReadAllText(path));
                if (!string.IsNullOrEmpty(config.serverAddress))
                    serverAddress = config.serverAddress.Trim();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("menu: config read failed, using default: " + e.Message);
        }
        ConnectionConfig.ServerAddress = serverAddress;
    }

    private void SetRegisterMode(bool register)
    {
        registerMode = register;
        formTitle.text = register ? "РЕГИСТРАЦИЯ" : "ВХОД";
        submitBtn.text = register ? "СОЗДАТЬ АККАУНТ" : "ВОЙТИ";
        modeLink.text = register ? "Уже есть аккаунт? Войти" : "Нет аккаунта? Создать";
        if (register)
        {
            confirmBlock.RemoveFromClassList("hidden");
            displayNameBlock.RemoveFromClassList("hidden");
        }
        else
        {
            confirmBlock.AddToClassList("hidden");
            displayNameBlock.AddToClassList("hidden");
        }
        loginStatus.text = "";
    }

    private IEnumerator CheckServer()
    {
        serverOk = false;
        cornerStatus.text = "";

        yield return Request("GET", "/api/server", null, false, (code, text) =>
        {
            if (code == 200)
            {
                ServerInfoResponse info = null;
                try { info = JsonUtility.FromJson<ServerInfoResponse>(text); } catch { }
                if (info != null && !string.IsNullOrEmpty(info.name))
                {
                    serverOk = true;
                    cornerStatus.text = info.name + " v" + info.major + "." + info.minor + "." + info.patch;
                    serverErrorScreen.AddToClassList("hidden");
                    if (worldsScreen.ClassListContains("hidden"))
                        loginScreen.RemoveFromClassList("hidden");
                    return;
                }
            }

            cornerStatus.text = "";
            serverErrorDetail.text = "По адресу " + serverAddress + " никто не ответил — или это не сервер Shooter. Адрес лежит в StreamingAssets/config.json.";
            loginScreen.AddToClassList("hidden");
            worldsScreen.AddToClassList("hidden");
            serverErrorScreen.RemoveFromClassList("hidden");
        });
    }

    private void Submit()
    {
        if (busy || !serverOk) return;
        loginStatus.text = "";

        string username = usernameField.value.Trim();
        string password = passwordField.value;
        string displayName = registerMode ? displayNameField.value.Trim() : username;

        if (!Regex.IsMatch(username, "^[a-zA-Z0-9_]{4,20}$")) { loginStatus.text = "Имя пользователя: 4-20 символов, латиница, цифры, подчёркивание"; return; }
        if (password.Length < 8 || password.Length > 40) { loginStatus.text = "Пароль: 8-40 символов"; return; }
        bool upper = false, lower = false, digit = false;
        foreach (char c in password)
        {
            if (char.IsUpper(c)) upper = true;
            else if (char.IsLower(c)) lower = true;
            else if (char.IsDigit(c)) digit = true;
        }
        if (!upper || !lower || !digit) { loginStatus.text = "В пароле нужны заглавная, строчная и цифра"; return; }
        if (registerMode)
        {
            if (passwordField.value != confirmField.value) { loginStatus.text = "Пароли не совпадают"; return; }
            if (displayName.Length < 1 || displayName.Length > 40) { loginStatus.text = "Отображаемое имя: 1-40 символов"; return; }
        }

        StartCoroutine(Authenticate(username, displayName, password));
    }

    private IEnumerator Authenticate(string username, string displayName, string password)
    {
        busy = true;
        submitBtn.SetEnabled(false);

        string path = registerMode ? "/api/auth/register" : "/api/auth/login";
        string body = registerMode
            ? JsonUtility.ToJson(new RegisterRequest { username = username, displayName = displayName, password = password })
            : JsonUtility.ToJson(new LoginRequest { username = username, password = password });

        yield return Request("POST", path, body, false, (code, text) =>
        {
            busy = false;
            submitBtn.SetEnabled(true);

            if (code != 200 && code != 201) { loginStatus.text = HumanError(code, text); return; }

            string token = JsonUtility.FromJson<TokenResponse>(text).token;
            if (string.IsNullOrEmpty(token)) { loginStatus.text = "Сервер не прислал токен"; return; }

            ConnectionConfig.Username = username;
            ConnectionConfig.DisplayName = displayName;
            ConnectionConfig.Token = token;
            myUserId = ExtractUserId(token);

            PlayerPrefs.SetString("username", username);
            PlayerPrefs.Save();

            loginStatus.text = "";
            userLabel.text = displayName;
            loginScreen.AddToClassList("hidden");
            worldsScreen.RemoveFromClassList("hidden");
            ReloadWorlds();
        });
    }

    private static string HumanError(long code, string text)
    {
        if (code == 0) return "Нет соединения с сервером";
        try
        {
            var problem = JsonUtility.FromJson<ProblemResponse>(text);
            if (!string.IsNullOrEmpty(problem.code))
            {
                if (ErrorTexts.TryGetValue(problem.code, out string human)) return human;
                if (!string.IsNullOrEmpty(problem.detail)) return problem.detail;
                return problem.code;
            }
        }
        catch { }
        return "Ошибка " + code;
    }

    private static long ExtractUserId(string token)
    {
        try
        {
            string[] parts = token.Split('.');
            string payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4) { case 2: payload += "=="; break; case 3: payload += "="; break; }
            var claims = JsonUtility.FromJson<JwtClaims>(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            return long.Parse(claims.sub.Split(':')[0]);
        }
        catch { return -1; }
    }

    private void SwitchTab(bool mine)
    {
        mineTab = mine;
        if (mine) { tabMine.AddToClassList("list-tab-active"); tabPublic.RemoveFromClassList("list-tab-active"); }
        else { tabPublic.AddToClassList("list-tab-active"); tabMine.RemoveFromClassList("list-tab-active"); }
        ReloadWorlds();
    }

    private void ReloadWorlds()
    {
        page = 0;
        StartCoroutine(LoadWorlds(true));
    }

    private IEnumerator LoadWorlds(bool reset)
    {
        if (reset) worldsScroll.Clear();
        worldsStatus.text = "";

        string path = "/api/worlds?page=" + page + "&size=" + PageSize;
        if (mineTab) path += "&playerRole=CREATOR";

        yield return Request("GET", path, null, true, (code, text) =>
        {
            if (code != 200) { worldsStatus.text = HumanError(code, text); return; }

            WorldDto[] worlds;
            try { worlds = JsonUtility.FromJson<WorldsWrap>("{\"items\":" + text + "}").items; }
            catch { worldsStatus.text = "Не смог прочитать список миров"; return; }

            foreach (WorldDto world in worlds)
                worldsScroll.Add(BuildCard(world));

            loadMoreBtn.style.display = worlds.Length < PageSize ? DisplayStyle.None : DisplayStyle.Flex;

            if (worlds.Length == 0 && page == 0)
            {
                var empty = new Label(mineTab ? "Пока пусто — создай свой первый мир" : "Публичных миров пока нет");
                empty.AddToClassList("empty-note");
                worldsScroll.Add(empty);
            }
        });
    }

    private VisualElement BuildCard(WorldDto world)
    {
        var card = new VisualElement();
        card.AddToClassList("world-card");
        if (world.visibilityPolicy == "PRIVATE")
            card.AddToClassList("world-card-private");

        var info = new VisualElement();
        info.AddToClassList("world-info");

        var nameRow = new VisualElement();
        nameRow.AddToClassList("world-name-row");
        var name = new Label(world.name);
        name.AddToClassList("world-name");
        nameRow.Add(name);

        if (world.visibilityPolicy == "PRIVATE")
            nameRow.Add(MakeBadge("СКРЫТЫЙ", "badge-private"));

        string myRole = FindMyRole(world);
        if (myRole == "CREATOR") nameRow.Add(MakeBadge("СОЗДАТЕЛЬ", "badge-creator"));
        else if (myRole == "MODERATOR") nameRow.Add(MakeBadge("МОДЕРАТОР", "badge-moderator"));
        else if (myRole == "MEMBER") nameRow.Add(MakeBadge("УЧАСТНИК", "badge-member"));

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

        var joinBtn = new Button(() => StartCoroutine(Join(world.id))) { text = "ИГРАТЬ" };
        joinBtn.AddToClassList("btn");
        joinBtn.AddToClassList("px");
        joinBtn.AddToClassList("play-btn");
        card.Add(joinBtn);

        return card;
    }

    private static Label MakeBadge(string text, string styleClass)
    {
        var badge = new Label(text);
        badge.AddToClassList("badge");
        badge.AddToClassList("px");
        badge.AddToClassList(styleClass);
        return badge;
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
                if (p.role == "CREATOR" && p.user != null) { creator = " · создал " + p.user.displayName; break; }

        long age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - world.createdAt;
        string ago = age < 3600 ? Math.Max(1, age / 60) + " мин назад" : age < 86400 ? (age / 3600) + " ч назад" : (age / 86400) + " дн назад";
        string players = count + (count % 10 == 1 && count % 100 != 11 ? " игрок" : (count % 10 >= 2 && count % 10 <= 4 && (count % 100 < 12 || count % 100 > 14) ? " игрока" : " игроков"));
        return players + " · " + ago + creator;
    }

    private IEnumerator Join(string worldId)
    {
        if (busy) yield break;
        busy = true;
        worldsStatus.text = "";

        yield return Request("POST", "/api/worlds/" + worldId + "/players", "{}", true, (code, text) =>
        {
            busy = false;
            if (code != 200 && code != 201) { worldsStatus.text = HumanError(code, text); return; }

            string worldToken = JsonUtility.FromJson<TokenResponse>(text).token;
            if (string.IsNullOrEmpty(worldToken)) { worldsStatus.text = "Сервер не прислал токен мира"; return; }

            ConnectionConfig.WorldToken = worldToken;
            ConnectionConfig.RoomCode = worldId;
            SceneManager.LoadScene(GameSceneName);
        });
    }

    private IEnumerator CreateWorld()
    {
        string name = createNameField.value.Trim();
        string desc = createDescField.value ?? "";
        if (name.Length < 1) { createStatus.text = "Сначала назови мир"; yield break; }

        createStatus.text = "";
        string body = JsonUtility.ToJson(new CreateWorldRequest
        {
            name = name,
            description = desc,
            visibilityPolicy = VisibilityValues[Mathf.Clamp(createVisibility.index, 0, VisibilityValues.Length - 1)],
            joinPolicy = PolicyValues[Mathf.Clamp(createPolicy.index, 0, PolicyValues.Length - 1)]
        });

        yield return Request("POST", "/api/worlds", body, true, (code, text) =>
        {
            if (code != 200 && code != 201) { createStatus.text = HumanError(code, text); return; }
            createModal.AddToClassList("hidden");
            createNameField.value = "";
            createDescField.value = "";
            if (!mineTab) SwitchTab(true);
            else ReloadWorlds();
        });
    }

    private IEnumerator Request(string method, string path, string body, bool auth, Action<long, string> onDone)
    {
        string url = "http://" + serverAddress + path;
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
