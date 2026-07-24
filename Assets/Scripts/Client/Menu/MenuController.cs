using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Shooter.Logging;
using Shooter.Client.Account;

namespace Shooter.Client.Menu
{
    [RequireComponent(typeof(UIDocument))]
    public class MenuController : MonoBehaviour
    {
        private ClientSession session;
        private MenuApi api;
        private MenuBackground background;
        private LoginScreen login;
        private ServerErrorScreen serverError;
        private WorldsScreen worlds;
        private CreateWorldModal createModal;
        private ErrorModal errorModal;
        private Label cornerStatus;

        public void Bind(ClientSession clientSession)
        {
            session = clientSession;
        }

        private void Start()
        {
            Log.Info("MenuController starting...");
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;

            var root = GetComponent<UIDocument>().rootVisualElement;
            background = new MenuBackground();
            root.Q<VisualElement>("root").Insert(0, background);
            cornerStatus = root.Q<Label>("corner-status");

            api = new MenuApi(this, session);
            errorModal = new ErrorModal(root);
            serverError = new ServerErrorScreen(root, CheckServer);
            login = new LoginScreen(root, api, session, OnLoggedIn);
            worlds = new WorldsScreen(root, api, errorModal, session, onCreateClick: () => createModal.Show(), onJoined: OnJoined);
            createModal = new CreateWorldModal(root, api, onCreated: () => worlds.Reload());

            ShowHome();
            CheckServer();
        }

        private void ShowHome()
        {
            if (!session.LoggedIn)
            {
                worlds.Hide();
                login.Show();
            }
            else
            {
                login.Hide();
                worlds.Show();
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
                    serverError.Show("Сервер по адресу " + session.ServerAddress + " недоступен. Адрес задаётся в файле StreamingAssets/config.json.");
                    return;
                }

                cornerStatus.text = info.Name + " v" + info.Major + "." + info.Minor + "." + info.Patch;
                serverError.Hide();
                ShowHome();
            });
        }

        private void Update()
        {
            background.Tick(Time.deltaTime);

            if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;
            Log.Info("Escape pressed, quitting");
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
