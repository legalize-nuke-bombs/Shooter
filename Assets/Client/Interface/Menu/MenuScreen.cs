using System;
using Shooter.Configuring;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class MenuScreen : Overlay
    {
        private const string PanelElement = "panel";
        private const string VersionElement = "version";
        private const string MainElement = "page-main";
        private const string NewGameElement = "page-new";
        private const string SavesElement = "page-saves";
        private const string ClientElement = "page-client";
        private const string WideClass = "menu__panel--wide";
        private static readonly Journal Log = Logs.Here();
        private readonly PageStack pages = new();
        private ClientPage client;
        private Dialog dialog;
        private MainPage main;
        private NewGamePage newGame;
        private VisualElement panel;
        private SavesPage saves;

        public event Action<string> Hosting;

        public event Action Joining;

        public event Action Quitting;

        private void Update()
        {
            if (!Bound) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

            if (dialog.Open) dialog.Cancel();
            else pages.Pop();
        }

        protected override bool Bind(VisualElement root)
        {
            PortConverters.Register();
            root.dataSource = Config.Read();

            panel = root.Q<VisualElement>(PanelElement);
            Label version = root.Q<Label>(VersionElement);
            VisualElement mainRoot = root.Q<VisualElement>(MainElement);
            VisualElement newGameRoot = root.Q<VisualElement>(NewGameElement);
            VisualElement savesRoot = root.Q<VisualElement>(SavesElement);
            VisualElement clientRoot = root.Q<VisualElement>(ClientElement);

            if (panel == null || version == null || mainRoot == null || newGameRoot == null || savesRoot == null ||
                clientRoot == null)
            {
                Log.Error("Menu document is incomplete, the menu stays dead");
                return false;
            }

            try
            {
                dialog = new Dialog(root);
                main = new MainPage(mainRoot);
                newGame = new NewGamePage(newGameRoot);
                saves = new SavesPage(savesRoot, dialog);
                client = new ClientPage(clientRoot);
            }
            catch (InvalidOperationException e)
            {
                Log.Error($"{e.Message}, the menu stays dead");
                return false;
            }

            version.text = Application.version;

            main.Continuing += Host;
            main.NewGameOpening += OpenNewGame;
            main.SavesOpening += OpenSaves;
            main.JoinOpening += OpenClient;
            main.Quitting += Quit;
            newGame.Starting += HostFresh;
            newGame.Backing += Back;
            saves.Loading += Host;
            saves.Backing += Back;
            client.Connecting += Connect;
            client.Backing += Back;

            pages.Changed += Resize;
            pages.Push(main);

            return true;
        }

        protected override void Unbind()
        {
            pages.Changed -= Resize;
            pages.Clear();

            dialog = null;
            main = null;
            newGame = null;
            saves = null;
            client = null;
            panel = null;
        }

        private void OpenNewGame()
        {
            pages.Push(newGame);
        }

        private void OpenSaves()
        {
            pages.Push(saves);
        }

        private void OpenClient()
        {
            pages.Push(client);
        }

        private void Back()
        {
            pages.Pop();
        }

        private void Resize(MenuPage page)
        {
            panel.EnableInClassList(WideClass, page.Wide);
        }

        private void HostFresh()
        {
            Log.Info("The player starts a fresh game");
            Hosting?.Invoke(null);
        }

        private void Host(string save)
        {
            Log.Info($"The player continues the game saved at {save}");
            Hosting?.Invoke(save);
        }

        private void Connect()
        {
            Log.Info("The player joins a game of someone else");
            Joining?.Invoke();
        }

        private void Quit()
        {
            Quitting?.Invoke();
        }
    }
}
