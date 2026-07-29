using System;
using Shooter.Client.Interface.Overlays;
using Shooter.Configuring;
using Shooter.Logging;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface.Menu
{
    public class MenuScreen : Overlay
    {
        private const string MainScreen = "main";
        private const string HostScreen = "host";
        private const string JoinScreen = "join";
        private const string SettingsScreen = "settings";
        private const string OpenHostButton = "open-host";
        private const string OpenJoinButton = "open-join";
        private const string OpenSettingsButton = "open-settings";
        private const string QuitButton = "quit";
        private const string StartButton = "start";
        private const string ConnectButton = "connect";
        private const string HostBackButton = "host-back";
        private const string JoinBackButton = "join-back";
        private const string SettingsBackButton = "settings-back";
        private const string HostNameField = "host-name";
        private const string WorldField = "world";
        private const string HostPortField = "host-port";
        private const string JoinNameField = "join-name";
        private const string AddressField = "address";
        private const string JoinPortField = "join-port";

        private VisualElement mainScreen;
        private VisualElement hostScreen;
        private VisualElement joinScreen;
        private VisualElement settingsScreen;
        private Button openHost;
        private Button openJoin;
        private Button openSettings;
        private Button quit;
        private Button start;
        private Button connect;
        private Button hostBack;
        private Button joinBack;
        private Button settingsBack;
        private TextField hostName;
        private TextField world;
        private TextField hostPort;
        private TextField joinName;
        private TextField address;
        private TextField joinPort;
        private bool missing;

        public event Action Hosting;

        public event Action Joining;

        public event Action Quitting;

        protected override bool Bind(VisualElement root)
        {
            missing = false;

            mainScreen = Find<VisualElement>(root, MainScreen);
            hostScreen = Find<VisualElement>(root, HostScreen);
            joinScreen = Find<VisualElement>(root, JoinScreen);
            settingsScreen = Find<VisualElement>(root, SettingsScreen);
            openHost = Find<Button>(root, OpenHostButton);
            openJoin = Find<Button>(root, OpenJoinButton);
            openSettings = Find<Button>(root, OpenSettingsButton);
            quit = Find<Button>(root, QuitButton);
            start = Find<Button>(root, StartButton);
            connect = Find<Button>(root, ConnectButton);
            hostBack = Find<Button>(root, HostBackButton);
            joinBack = Find<Button>(root, JoinBackButton);
            settingsBack = Find<Button>(root, SettingsBackButton);
            hostName = Find<TextField>(root, HostNameField);
            world = Find<TextField>(root, WorldField);
            hostPort = Find<TextField>(root, HostPortField);
            joinName = Find<TextField>(root, JoinNameField);
            address = Find<TextField>(root, AddressField);
            joinPort = Find<TextField>(root, JoinPortField);

            if (missing)
            {
                Log.Error("Menu document is incomplete, the menu stays dead");
                return false;
            }

            openHost.clicked += OpenHost;
            openJoin.clicked += OpenJoin;
            openSettings.clicked += OpenSettings;
            quit.clicked += Leave;
            start.clicked += Play;
            connect.clicked += Enter;
            hostBack.clicked += CloseHost;
            joinBack.clicked += CloseJoin;
            settingsBack.clicked += CloseSettings;

            Show(mainScreen);

            return true;
        }

        protected override void Unbind()
        {
            openHost.clicked -= OpenHost;
            openJoin.clicked -= OpenJoin;
            openSettings.clicked -= OpenSettings;
            quit.clicked -= Leave;
            start.clicked -= Play;
            connect.clicked -= Enter;
            hostBack.clicked -= CloseHost;
            joinBack.clicked -= CloseJoin;
            settingsBack.clicked -= CloseSettings;

            mainScreen = null;
            hostScreen = null;
            joinScreen = null;
            settingsScreen = null;
            openHost = null;
            openJoin = null;
            openSettings = null;
            quit = null;
            start = null;
            connect = null;
            hostBack = null;
            joinBack = null;
            settingsBack = null;
            hostName = null;
            world = null;
            hostPort = null;
            joinName = null;
            address = null;
            joinPort = null;
        }

        private void OpenHost()
        {
            GameConfig config = Config.Read();

            hostName.value = config.Client.Name;
            world.value = config.Server.World;
            hostPort.value = config.Server.Port.ToString();

            Show(hostScreen);
        }

        private void OpenJoin()
        {
            GameConfig config = Config.Read();

            joinName.value = config.Client.Name;
            address.value = config.Client.Address;
            joinPort.value = config.Client.Port.ToString();

            Show(joinScreen);
        }

        private void OpenSettings()
        {
            Show(settingsScreen);
        }

        private void CloseHost()
        {
            KeepHost();
            Show(mainScreen);
        }

        private void CloseJoin()
        {
            KeepJoin();
            Show(mainScreen);
        }

        private void CloseSettings()
        {
            Show(mainScreen);
        }

        private void Play()
        {
            KeepHost();
            Log.Info("The player starts a world of his own");
            Hosting?.Invoke();
        }

        private void Enter()
        {
            KeepJoin();
            Log.Info("The player joins a world of someone else");
            Joining?.Invoke();
        }

        private void Leave()
        {
            Quitting?.Invoke();
        }

        private void KeepHost()
        {
            GameConfig config = Config.Read();

            config.Client.Name = hostName.value;
            config.Server.World = world.value;
            config.Server.Port = Number(hostPort.value, config.Server.Port);
            Config.Save();

            Log.Info("Own world {} on port {} under the name {}",
                config.Server.World, config.Server.Port, config.Client.Name);
        }

        private void KeepJoin()
        {
            GameConfig config = Config.Read();

            config.Client.Name = joinName.value;
            config.Client.Address = address.value;
            config.Client.Port = Number(joinPort.value, config.Client.Port);
            Config.Save();

            Log.Info("World of {}:{} under the name {}",
                config.Client.Address, config.Client.Port, config.Client.Name);
        }

        private void Show(VisualElement screen)
        {
            mainScreen.style.display = screen == mainScreen ? DisplayStyle.Flex : DisplayStyle.None;
            hostScreen.style.display = screen == hostScreen ? DisplayStyle.Flex : DisplayStyle.None;
            joinScreen.style.display = screen == joinScreen ? DisplayStyle.Flex : DisplayStyle.None;
            settingsScreen.style.display = screen == settingsScreen ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private T Find<T>(VisualElement root, string name) where T : VisualElement
        {
            var element = root.Q<T>(name);
            if (element != null) return element;

            Log.Error("Menu document has no element {}", name);
            missing = true;

            return null;
        }

        private static ushort Number(string typed, ushort fallback)
        {
            if (ushort.TryParse(typed, out ushort parsed) && parsed != 0) return parsed;

            Log.Warn("Port {} is not a number between 1 and 65535, keeping {}", typed, fallback);

            return fallback;
        }
    }
}
