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
        private const string HostElement = "page-host";
        private const string ClientElement = "page-client";
        private const string WideClass = "menu__panel--wide";
        private static readonly Journal Log = Logs.Here();
        private readonly PageStack pages = new();
        private ClientPage client;
        private HostPage host;
        private MainPage main;
        private VisualElement panel;

        public event Action<string> Hosting;

        public event Action Joining;

        public event Action Quitting;

        private void Update()
        {
            if (!Bound) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

            pages.Pop();
        }

        protected override bool Bind(VisualElement root)
        {
            PortConverters.Register();

            panel = root.Q<VisualElement>(PanelElement);
            Label version = root.Q<Label>(VersionElement);
            VisualElement mainRoot = root.Q<VisualElement>(MainElement);
            VisualElement hostRoot = root.Q<VisualElement>(HostElement);
            VisualElement clientRoot = root.Q<VisualElement>(ClientElement);

            if (panel == null || version == null || mainRoot == null || hostRoot == null || clientRoot == null)
            {
                Log.Error("Menu document is incomplete, the menu stays dead");
                return false;
            }

            try
            {
                main = new MainPage(mainRoot);
                host = new HostPage(hostRoot);
                client = new ClientPage(clientRoot);
            }
            catch (InvalidOperationException e)
            {
                Log.Error($"{e.Message}, the menu stays dead");
                return false;
            }

            root.dataSource = Config.Read();
            version.text = Application.version;

            main.HostOpening += OpenHost;
            main.ClientOpening += OpenClient;
            main.Quitting += Quit;
            host.Loading += Host;
            host.Starting += HostFresh;
            host.Backing += Back;
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

            main = null;
            host = null;
            client = null;
            panel = null;
        }

        private void OpenHost()
        {
            pages.Push(host);
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
