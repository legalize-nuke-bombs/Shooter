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
        private const string PageClass = "screen";
        private const string WideClass = "menu__panel--wide";
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private VisualTreeAsset mainPage;
        [SerializeField] private VisualTreeAsset newGamePage;
        [SerializeField] private VisualTreeAsset savesPage;
        [SerializeField] private VisualTreeAsset clientPage;

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

            if (panel == null || version == null)
            {
                Log.Error("Menu document is incomplete, the menu stays dead");
                return false;
            }

            if (mainPage == null || newGamePage == null || savesPage == null || clientPage == null)
            {
                Log.Error("Menu screen misses a page template, the menu stays dead");
                return false;
            }

            try
            {
                dialog = new Dialog(root);
                main = new MainPage(Mount(mainPage));
                newGame = new NewGamePage(Mount(newGamePage));
                saves = new SavesPage(Mount(savesPage), dialog);
                client = new ClientPage(Mount(clientPage));
            }
            catch (InvalidOperationException e)
            {
                Log.Error($"{e.Message}, the menu stays dead");
                return false;
            }

            version.text = Application.version;

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
            panel?.Clear();

            dialog = null;
            main = null;
            newGame = null;
            saves = null;
            client = null;
            panel = null;
        }

        private VisualElement Mount(VisualTreeAsset template)
        {
            TemplateContainer page = template.Instantiate();
            page.name = template.name;
            page.AddToClassList(PageClass);
            panel.Add(page);

            return page;
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
