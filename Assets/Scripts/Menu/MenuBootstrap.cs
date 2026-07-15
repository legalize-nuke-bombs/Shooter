using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class MenuBootstrap : MonoBehaviour
{
    private const string GameSceneName = "SampleScene";

    private string username;
    private string password = "";
    private string serverAddress;
    private string roomCode;
    private string status = "";
    private bool busy;
    private GUIStyle titleStyle;

    [Serializable]
    private class LoginRequestBody
    {
        public string username;
        public string password;
    }

    [Serializable]
    private class RegisterRequestBody
    {
        public string username;
        public string displayName;
        public string password;
    }

    [Serializable]
    private class TokenResponseBody
    {
        public string token;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        username = PlayerPrefs.GetString("username", "");
        serverAddress = PlayerPrefs.GetString("serverAddress", "localhost:8080");
        roomCode = PlayerPrefs.GetString("roomCode", "");
    }

    private void OnGUI()
    {
        titleStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        GUI.Label(new Rect(cx - 150f, cy - 210f, 300f, 50f), "SHOOTER", titleStyle);

        GUI.enabled = !busy;

        GUI.Label(new Rect(cx - 150f, cy - 140f, 110f, 24f), "Username");
        username = GUI.TextField(new Rect(cx - 30f, cy - 140f, 180f, 24f), username, 20);

        GUI.Label(new Rect(cx - 150f, cy - 108f, 110f, 24f), "Password");
        password = GUI.PasswordField(new Rect(cx - 30f, cy - 108f, 180f, 24f), password, '*', 64);

        GUI.Label(new Rect(cx - 150f, cy - 76f, 110f, 24f), "Server");
        serverAddress = GUI.TextField(new Rect(cx - 30f, cy - 76f, 180f, 24f), serverAddress, 64);

        GUI.Label(new Rect(cx - 150f, cy - 44f, 110f, 24f), "Room code");
        roomCode = GUI.TextField(new Rect(cx - 30f, cy - 44f, 180f, 24f), roomCode, 8);

        if (GUI.Button(new Rect(cx - 150f, cy + 4f, 145f, 40f), "LOGIN"))
            StartCoroutine(Authenticate(false));

        if (GUI.Button(new Rect(cx + 5f, cy + 4f, 145f, 40f), "REGISTER"))
            StartCoroutine(Authenticate(true));

        if (GUI.Button(new Rect(cx - 150f, cy + 54f, 300f, 28f), "QUIT"))
            Application.Quit();

        GUI.enabled = true;
        GUI.Label(new Rect(cx - 200f, cy + 94f, 400f, 48f), status);
    }

    private IEnumerator Authenticate(bool register)
    {
        if (username.Trim().Length == 0 || password.Length == 0)
        {
            status = "username and password required";
            yield break;
        }

        busy = true;
        status = register ? "registering..." : "logging in...";

        string url = "http://" + serverAddress.Trim() + (register ? "/api/auth/register" : "/api/auth/login");
        string body = register
            ? NetJson.Serialize(new RegisterRequestBody { username = username.Trim(), displayName = username.Trim(), password = password })
            : NetJson.Serialize(new LoginRequestBody { username = username.Trim(), password = password });

        using var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 10;

        yield return request.SendWebRequest();
        busy = false;

        if (request.result != UnityWebRequest.Result.Success)
        {
            status = "error " + request.responseCode + ": " + request.downloadHandler.text;
            yield break;
        }

        string token = NetJson.Parse<TokenResponseBody>(request.downloadHandler.text).token;
        if (string.IsNullOrEmpty(token))
        {
            status = "no token in response: " + request.downloadHandler.text;
            yield break;
        }

        ConnectionConfig.Username = username.Trim();
        ConnectionConfig.DisplayName = username.Trim();
        ConnectionConfig.Token = token;
        ConnectionConfig.ServerAddress = serverAddress.Trim();
        ConnectionConfig.RoomCode = roomCode.Trim();

        PlayerPrefs.SetString("username", ConnectionConfig.Username);
        PlayerPrefs.SetString("serverAddress", ConnectionConfig.ServerAddress);
        PlayerPrefs.SetString("roomCode", ConnectionConfig.RoomCode);
        PlayerPrefs.Save();

        SceneManager.LoadScene(GameSceneName);
    }
}
