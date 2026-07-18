using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Shooter.Logging;
using Shooter.Serialization;
using Shooter.Client.Account;

namespace Shooter.Client.Menu
{
    [RequireComponent(typeof(UIDocument))]
    public class MenuController : MonoBehaviour
    {
        private MenuApi api;
        private LoginScreen login;
        private ServerErrorScreen serverError;
        private WorldsScreen worlds;
        private CreateWorldModal createModal;
        private ErrorModal errorModal;
        private Label cornerStatus;

        private void Start()
        {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;

            LoadConfig();

            var root = GetComponent<UIDocument>().rootVisualElement;
            root.Q<VisualElement>("root").Insert(0, new MenuBackground());
            cornerStatus = root.Q<Label>("corner-status");

            api = new MenuApi(this);
            errorModal = new ErrorModal(root);
            serverError = new ServerErrorScreen(root, CheckServer);
            login = new LoginScreen(root, api, OnLoggedIn);
            worlds = new WorldsScreen(root, api, errorModal, onCreateClick: () => createModal.Show(), onJoined: OnJoined);
            createModal = new CreateWorldModal(root, api, onCreated: () => worlds.Reload());

            CheckServer();
        }

        private void LoadConfig()
        {
            try
            {
                string path = Path.Combine(Application.streamingAssetsPath, "config.json");
                if (File.Exists(path))
                {
                    var config = Json.Deserialize<ConfigFile>(File.ReadAllText(path));
                    if (!string.IsNullOrEmpty(config.ServerAddress))
                        Session.ServerAddress = config.ServerAddress.Trim();
                }
            }
            catch (Exception e)
            {
                Log.Warn("Menu: config read failed, using default: " + e.Message);
            }
        }

        private void CheckServer()
        {
            cornerStatus.text = "";

            api.CheckServer(info =>
            {
                if (info == null)
                {
                    login.Hide();
                    worlds.Hide();
                    serverError.Show("Сервер по адресу " + Session.ServerAddress + " недоступен. Адрес задаётся в файле StreamingAssets/config.json.");
                    return;
                }

                cornerStatus.text = info.Name + " v" + info.Major + "." + info.Minor + "." + info.Patch;
                serverError.Hide();
                if (string.IsNullOrEmpty(Session.Token)) login.Show();
                else worlds.Show();
            });
        }

        private void Update()
        {
            if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;
            Log.Info("Menu: Escape pressed, quitting");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }

        private void OnLoggedIn()
        {
            login.Hide();
            worlds.Show();
        }

        private void OnJoined()
        {
            SceneManager.LoadScene("Game");
        }
    }
}
