using System;
using Shooter.Client.Interface.Overlays;
using Shooter.Configuring;
using Shooter.Logging;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface.Menu
{
    public class MenuScreen : Overlay
    {
        private static readonly Journal Log = Logs.Here();

        private const string MainScreen = "main";
        private const string HostScreen = "host";
        private const string JoinScreen = "join";
        private const string SettingsScreen = "settings";
        private const string ClientSettings = "client-settings";
        private const string ServerSettings = "server-settings";
        private const string OpenHostButton = "open-host";
        private const string OpenJoinButton = "open-join";
        private const string OpenSettingsButton = "open-settings";
        private const string OpenClientButton = "open-client";
        private const string OpenServerButton = "open-server";
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
        private const string PlayerNameField = "player-name";
        private const string ServerAddressField = "server-address";
        private const string ClientPortField = "client-port";
        private const string WorldNameField = "world-name";
        private const string ServerPortField = "server-port";
        private const string LlmBaseProviderField = "llm-base-provider";
        private const string LlmBaseModelField = "llm-base-model";
        private const string LlmBaseKeyField = "llm-base-key";
        private const string LlmMaxProviderField = "llm-max-provider";
        private const string LlmMaxModelField = "llm-max-model";
        private const string LlmMaxKeyField = "llm-max-key";
        private const string ChosenTab = "menu__tab--chosen";

        private VisualElement mainScreen;
        private VisualElement hostScreen;
        private VisualElement joinScreen;
        private VisualElement settingsScreen;
        private VisualElement clientSettings;
        private VisualElement serverSettings;
        private Button openHost;
        private Button openJoin;
        private Button openSettings;
        private Button openClient;
        private Button openServer;
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
        private TextField playerName;
        private TextField serverAddress;
        private TextField clientPort;
        private TextField worldName;
        private TextField serverPort;
        private TextField llmBaseProvider;
        private TextField llmBaseModel;
        private TextField llmBaseKey;
        private TextField llmMaxProvider;
        private TextField llmMaxModel;
        private TextField llmMaxKey;
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
            clientSettings = Find<VisualElement>(root, ClientSettings);
            serverSettings = Find<VisualElement>(root, ServerSettings);
            openHost = Find<Button>(root, OpenHostButton);
            openJoin = Find<Button>(root, OpenJoinButton);
            openSettings = Find<Button>(root, OpenSettingsButton);
            openClient = Find<Button>(root, OpenClientButton);
            openServer = Find<Button>(root, OpenServerButton);
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
            playerName = Find<TextField>(root, PlayerNameField);
            serverAddress = Find<TextField>(root, ServerAddressField);
            clientPort = Find<TextField>(root, ClientPortField);
            worldName = Find<TextField>(root, WorldNameField);
            serverPort = Find<TextField>(root, ServerPortField);
            llmBaseProvider = Find<TextField>(root, LlmBaseProviderField);
            llmBaseModel = Find<TextField>(root, LlmBaseModelField);
            llmBaseKey = Find<TextField>(root, LlmBaseKeyField);
            llmMaxProvider = Find<TextField>(root, LlmMaxProviderField);
            llmMaxModel = Find<TextField>(root, LlmMaxModelField);
            llmMaxKey = Find<TextField>(root, LlmMaxKeyField);

            if (missing)
            {
                Log.Error("Menu document is incomplete, the menu stays dead");
                return false;
            }

            openHost.clicked += OpenHost;
            openJoin.clicked += OpenJoin;
            openSettings.clicked += OpenSettings;
            openClient.clicked += ShowClient;
            openServer.clicked += ShowServer;
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
            openClient.clicked -= ShowClient;
            openServer.clicked -= ShowServer;
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
            clientSettings = null;
            serverSettings = null;
            openHost = null;
            openJoin = null;
            openSettings = null;
            openClient = null;
            openServer = null;
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
            playerName = null;
            serverAddress = null;
            clientPort = null;
            worldName = null;
            serverPort = null;
            llmBaseProvider = null;
            llmBaseModel = null;
            llmBaseKey = null;
            llmMaxProvider = null;
            llmMaxModel = null;
            llmMaxKey = null;
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
            GameConfig config = Config.Read();

            playerName.value = config.Client.Name;
            serverAddress.value = config.Client.Address;
            clientPort.value = config.Client.Port.ToString();
            worldName.value = config.Server.World;
            serverPort.value = config.Server.Port.ToString();
            llmBaseProvider.value = config.Server.LlmBase.Provider;
            llmBaseModel.value = config.Server.LlmBase.Model;
            llmBaseKey.value = config.Server.LlmBase.Key;
            llmMaxProvider.value = config.Server.LlmMax.Provider;
            llmMaxModel.value = config.Server.LlmMax.Model;
            llmMaxKey.value = config.Server.LlmMax.Key;

            ShowClient();
            Show(settingsScreen);
        }

        private void ShowClient()
        {
            Choose(clientSettings);
        }

        private void ShowServer()
        {
            Choose(serverSettings);
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
            KeepSettings();
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

            Log.Info($"Own world {config.Server.World} on port {config.Server.Port} under the name {config.Client.Name}");
        }

        private void KeepJoin()
        {
            GameConfig config = Config.Read();

            config.Client.Name = joinName.value;
            config.Client.Address = address.value;
            config.Client.Port = Number(joinPort.value, config.Client.Port);
            Config.Save();

            Log.Info($"World of {config.Client.Address}:{config.Client.Port} under the name {config.Client.Name}");
        }

        private void KeepSettings()
        {
            GameConfig config = Config.Read();

            config.Client.Name = playerName.value;
            config.Client.Address = serverAddress.value;
            config.Client.Port = Number(clientPort.value, config.Client.Port);
            config.Server.World = worldName.value;
            config.Server.Port = Number(serverPort.value, config.Server.Port);
            config.Server.LlmBase.Provider = llmBaseProvider.value;
            config.Server.LlmBase.Model = llmBaseModel.value;
            config.Server.LlmBase.Key = llmBaseKey.value;
            config.Server.LlmMax.Provider = llmMaxProvider.value;
            config.Server.LlmMax.Model = llmMaxModel.value;
            config.Server.LlmMax.Key = llmMaxKey.value;
            Config.Save();

            Log.Info("Settings kept");
        }

        private void Choose(VisualElement tab)
        {
            clientSettings.style.display = tab == clientSettings ? DisplayStyle.Flex : DisplayStyle.None;
            serverSettings.style.display = tab == serverSettings ? DisplayStyle.Flex : DisplayStyle.None;

            openClient.EnableInClassList(ChosenTab, tab == clientSettings);
            openServer.EnableInClassList(ChosenTab, tab == serverSettings);
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

            Log.Error($"Menu document has no element {name}");
            missing = true;

            return null;
        }

        private static ushort Number(string typed, ushort fallback)
        {
            if (ushort.TryParse(typed, out ushort parsed) && parsed != 0) return parsed;

            Log.Warn($"Port {typed} is not a number between 1 and 65535, keeping {fallback}");

            return fallback;
        }
    }
}
