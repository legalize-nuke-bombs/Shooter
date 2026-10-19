using System;
using System.Threading.Tasks;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class MainMenuOverlay : Overlay
    {
        private const string PanelElement = "panel";
        private const string VersionElement = "version";
        private const string PageClass = "screen";
        private const int MenuFrameRate = 60;
        private const int UnlimitedFrameRate = -1;
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private VisualTreeAsset mainPage;
        [SerializeField] private VisualTreeAsset newGamePage;
        [SerializeField] private VisualTreeAsset savesPage;
        [SerializeField] private VisualTreeAsset clientPage;
        [SerializeField] private VisualTreeAsset serverSettingsPage;
        [SerializeField] private VisualTreeAsset clientSettingsPage;
        [SerializeField] private VisualTreeAsset accountPage;

        private readonly PageStack pages = new();
        private AccountPage account;
        private ClientPage client;
        private ClientSettingsPage clientSettings;
        private Dialog dialog;
        private LoadingScreenOverlay loading;
        private MainPage main;
        private NewGamePage newGame;
        private VisualElement panel;
        private SavesPage saves;
        private ServerSettingsPage serverSettings;
        private NetworkStarter starter;
        private string warnDetails;
        private string warnTitle;

        private void Awake()
        {
            Application.targetFrameRate = MenuFrameRate;
            starter = NetworkManager.Singleton.GetComponent<NetworkStarter>();
        }

        // A world that ended halfway through its load leaves the loading screen up, the menu takes it down
        private void Start()
        {
            loading = FindAnyObjectByType<LoadingScreenOverlay>();
            loading.Hide();
            starter.JoinFailed += JoinFailed;
        }

        private void OnDestroy()
        {
            starter.JoinFailed -= JoinFailed;
            Application.targetFrameRate = UnlimitedFrameRate;
        }

        private void Warn(string title, string details)
        {
            warnTitle = title;
            warnDetails = details;
            ShowWarning();
        }

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

            if (mainPage == null || newGamePage == null || savesPage == null || clientPage == null || serverSettingsPage == null || clientSettingsPage == null || accountPage == null)
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
                serverSettings = new ServerSettingsPage(Mount(serverSettingsPage));
                clientSettings = new ClientSettingsPage(Mount(clientSettingsPage));
                account = new AccountPage(Mount(accountPage));
            }
            catch (InvalidOperationException e)
            {
                Log.Error($"{e.Message}, the menu stays dead");
                return false;
            }

            version.text = Application.version;

            main.NewGameOpening += OpenNewGame;
            main.SavesOpening += OpenSaves;
            main.ServerSettingsOpening += OpenServerSettings;
            main.ClientSettingsOpening += OpenClientSettings;
            main.JoinOpening += OpenClient;
            main.AccountOpening += OpenAccount;
            main.Quitting += Quit;
            newGame.Starting += HostFresh;
            newGame.Backing += Back;
            saves.Loading += Host;
            saves.Backing += Back;
            client.Connecting += Connect;
            client.Backing += Back;
            serverSettings.Backing += Back;
            clientSettings.Backing += Back;
            account.Backing += Back;

            pages.Push(main);

            if (starter.Failure != null)
            {
                warnTitle = starter.Failure.Title;
                warnDetails = starter.Failure.Details;
                starter.Failure = null;
            }

            ShowWarning();

            if (Config.Read().Account == null) GenerateKey();

            Log.Info("Menu is up");
            return true;
        }

        private void ShowWarning()
        {
            if (dialog == null || warnTitle == null) return;

            dialog.Notice(warnTitle, warnDetails, "Понятно");
            warnTitle = null;
            warnDetails = null;
        }

        private async void GenerateKey()
        {
            dialog.Busy("Создание аккаунта...", "Пожалуйста, подождите. Может занять несколько секунд");
            try
            {
                Account account = await Task.Run(() => Account.Generate());
                Config.Read().Account = account;
                Config.Save();
                Log.Info("Account key generated");
            }
            catch (Exception e)
            {
                Log.Error($"Account key generation failed: {e.Message}");
            }

            dialog?.Release();
        }

        protected override void Unbind()
        {
            pages.Clear();
            panel?.Clear();

            dialog = null;
            main = null;
            newGame = null;
            saves = null;
            client = null;
            serverSettings = null;
            clientSettings = null;
            account = null;
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

        private void OpenServerSettings()
        {
            pages.Push(serverSettings);
        }

        private void OpenClientSettings()
        {
            pages.Push(clientSettings);
        }

        private void OpenAccount()
        {
            pages.Push(account);
        }

        private void Back()
        {
            pages.Pop();
        }

        private void HostFresh()
        {
            Log.Info("The player starts a fresh game");
            Host(null);
        }

        private void Host(string save)
        {
            if (save != null) Log.Info($"The player continues the game saved at {save}");
            loading.Show(LoadingScreenStage.Server);
            NetworkStartFailure failure = starter.Host(save);
            if (failure == null) return;

            loading.Hide();
            Warn(failure.Title, failure.Details);
        }

        private void Connect()
        {
            Log.Info("The player joins a game of someone else");
            NetworkStartFailure failure = starter.Join();
            if (failure != null)
            {
                Warn(failure.Title, failure.Details);
                return;
            }

            loading.Show(LoadingScreenStage.Connection, Cancel);
        }

        private void Cancel()
        {
            starter.Cancel();
            loading.Hide();
        }

        private void JoinFailed(NetworkStartFailure failure)
        {
            loading.Hide();
            Warn(failure.Title, failure.Details);
        }

        private void Quit()
        {
            Log.Info("Leaving the game by the player's own hand");
            Application.Quit();
        }
    }
}
