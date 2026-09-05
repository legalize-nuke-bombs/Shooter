using System;
using System.Threading.Tasks;
using Shooter.Accounts;
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
        private MainPage main;
        private NewGamePage newGame;
        private VisualElement panel;
        private SavesPage saves;
        private ServerSettingsPage serverSettings;
        private string warnDetails;
        private string warnTitle;

        public event Action<string> Hosting;

        public event Action Joining;

        public event Action Quitting;

        public void Warn(string title, string details)
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

            ShowWarning();

            if (Config.Read().Account == null) GenerateKey();

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
