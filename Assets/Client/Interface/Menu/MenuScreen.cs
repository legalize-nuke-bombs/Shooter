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
        private const string SettingsScreen = "settings";
        private const string HostButton = "host";
        private const string JoinButton = "join";
        private const string OpenSettingsButton = "open-settings";
        private const string QuitButton = "quit";
        private const string BackButton = "back";
        private const string NameField = "player-name";
        private const string AddressField = "address";
        private const string PortField = "port";
        private const string WorldField = "world";
        private const string ServerPortField = "server-port";

        private VisualElement main;
        private VisualElement settings;
        private Button host;
        private Button join;
        private Button openSettings;
        private Button quit;
        private Button back;
        private TextField playerName;
        private TextField address;
        private TextField port;
        private TextField world;
        private TextField serverPort;

        public event Action Hosting;

        public event Action Joining;

        public event Action Quitting;

        protected override bool Bind(VisualElement root)
        {
            main = root.Q<VisualElement>(MainScreen);
            settings = root.Q<VisualElement>(SettingsScreen);
            host = root.Q<Button>(HostButton);
            join = root.Q<Button>(JoinButton);
            openSettings = root.Q<Button>(OpenSettingsButton);
            quit = root.Q<Button>(QuitButton);
            back = root.Q<Button>(BackButton);
            playerName = root.Q<TextField>(NameField);
            address = root.Q<TextField>(AddressField);
            port = root.Q<TextField>(PortField);
            world = root.Q<TextField>(WorldField);
            serverPort = root.Q<TextField>(ServerPortField);

            if (main == null || settings == null || host == null || join == null || openSettings == null ||
                quit == null || back == null || playerName == null || address == null || port == null ||
                world == null || serverPort == null)
            {
                Log.Error("Menu document is missing elements, the menu stays dead");
                return false;
            }

            host.clicked += Play;
            join.clicked += Connect;
            openSettings.clicked += Open;
            quit.clicked += Leave;
            back.clicked += Close;

            Fill();
            Show(main);

            return true;
        }

        protected override void Unbind()
        {
            host.clicked -= Play;
            join.clicked -= Connect;
            openSettings.clicked -= Open;
            quit.clicked -= Leave;
            back.clicked -= Close;

            main = null;
            settings = null;
            host = null;
            join = null;
            openSettings = null;
            quit = null;
            back = null;
            playerName = null;
            address = null;
            port = null;
            world = null;
            serverPort = null;
        }

        private void Play()
        {
            Log.Info("The player starts a world of his own");
            Hosting?.Invoke();
        }

        private void Connect()
        {
            Log.Info("The player joins a world of someone else");
            Joining?.Invoke();
        }

        private void Leave()
        {
            Quitting?.Invoke();
        }

        private void Open()
        {
            Show(settings);
        }

        private void Close()
        {
            Apply();
            Config.Save();
            Show(main);
        }

        private void Fill()
        {
            GameConfig config = Config.Read();

            playerName.value = config.Client.Name;
            address.value = config.Client.Address;
            port.value = config.Client.Port.ToString();
            world.value = config.Server.World;
            serverPort.value = config.Server.Port.ToString();
        }

        private void Apply()
        {
            GameConfig config = Config.Read();

            config.Client.Name = playerName.value;
            config.Client.Address = address.value;
            config.Client.Port = Number(port.value, config.Client.Port);
            config.Server.World = world.value;
            config.Server.Port = Number(serverPort.value, config.Server.Port);

            Log.Info("Settings taken from the menu: {} heads for {}:{}, own world {} on port {}",
                config.Client.Name, config.Client.Address, config.Client.Port,
                config.Server.World, config.Server.Port);
        }

        private void Show(VisualElement screen)
        {
            main.style.display = screen == main ? DisplayStyle.Flex : DisplayStyle.None;
            settings.style.display = screen == settings ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static ushort Number(string typed, ushort fallback)
        {
            if (ushort.TryParse(typed, out ushort parsed) && parsed != 0) return parsed;

            Log.Warn("Port {} is not a number between 1 and 65535, keeping {}", typed, fallback);

            return fallback;
        }
    }
}
