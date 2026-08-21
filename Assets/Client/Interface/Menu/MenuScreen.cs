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
        private const string WorldsElement = "page-worlds";
        private const string JoinElement = "page-join";
        private const string SettingsElement = "page-settings";
        private const string WideClass = "menu__panel--wide";
        private static readonly Journal Log = Logs.Here();
        private readonly PageStack pages = new();
        private JoinPage join;
        private MainPage main;
        private VisualElement panel;
        private SettingsPage settings;
        private WorldsPage worlds;

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
            VisualElement worldsRoot = root.Q<VisualElement>(WorldsElement);
            VisualElement joinRoot = root.Q<VisualElement>(JoinElement);
            VisualElement settingsRoot = root.Q<VisualElement>(SettingsElement);

            if (panel == null || version == null || mainRoot == null || worldsRoot == null || joinRoot == null ||
                settingsRoot == null)
            {
                Log.Error("Menu document is incomplete, the menu stays dead");
                return false;
            }

            try
            {
                main = new MainPage(mainRoot);
                worlds = new WorldsPage(worldsRoot);
                join = new JoinPage(joinRoot);
                settings = new SettingsPage(settingsRoot);
            }
            catch (InvalidOperationException e)
            {
                Log.Error($"{e.Message}, the menu stays dead");
                return false;
            }

            root.dataSource = Config.Read();
            version.text = Application.version;

            main.Continuing += Host;
            main.Browsing += OpenWorlds;
            main.Starting += HostFresh;
            main.Joining += OpenJoin;
            main.Configuring += OpenSettings;
            main.Quitting += Quit;
            worlds.Loading += Host;
            worlds.Backing += Back;
            join.Connecting += Connect;
            join.Backing += Back;
            settings.Backing += Back;

            pages.Changed += Resize;
            pages.Push(main);

            return true;
        }

        protected override void Unbind()
        {
            pages.Changed -= Resize;
            pages.Clear();

            main = null;
            worlds = null;
            join = null;
            settings = null;
            panel = null;
        }

        private void OpenWorlds()
        {
            pages.Push(worlds);
        }

        private void OpenJoin()
        {
            pages.Push(join);
        }

        private void OpenSettings()
        {
            pages.Push(settings);
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
            Log.Info("The player starts a fresh world");
            Hosting?.Invoke(null);
        }

        private void Host(string save)
        {
            Log.Info($"The player continues the world saved at {save}");
            Hosting?.Invoke(save);
        }

        private void Connect()
        {
            Log.Info("The player joins a world of someone else");
            Joining?.Invoke();
        }

        private void Quit()
        {
            Quitting?.Invoke();
        }
    }
}
