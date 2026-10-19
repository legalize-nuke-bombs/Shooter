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
        private const string PanelElementName = "panel";
        private const string VersionLabelName = "version";
        private const string PageClass = "screen";
        private const int MenuFrameRate = 60;
        private const int UnlimitedFrameRate = -1;
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        [SerializeField] private VisualTreeAsset mainPage;
        [SerializeField] private VisualTreeAsset newGamePage;
        [SerializeField] private VisualTreeAsset savesPage;
        [SerializeField] private VisualTreeAsset clientPage;
        [SerializeField] private VisualTreeAsset serverSettingsPage;
        [SerializeField] private VisualTreeAsset clientSettingsPage;
        [SerializeField] private VisualTreeAsset accountPage;

        private readonly MainMenuPageStack pageStack = new();
        private AccountPage accountPageController;
        private ClientPage clientPageController;
        private ClientSettingsPage clientSettingsPageController;
        private MainMenuDialog dialog;
        private LoadingScreenOverlay loadingScreenOverlay;
        private MainPage mainPageController;
        private NewGamePage newGamePageController;
        private VisualElement panelElement;
        private SavesPage savesPageController;
        private ServerSettingsPage serverSettingsPageController;
        private NetworkStarter networkStarter;
        private string pendingWarningDetails;
        private string pendingWarningTitle;

        private void Awake()
        {
            Application.targetFrameRate = MenuFrameRate;
            networkStarter = NetworkManager.Singleton.GetComponent<NetworkStarter>();
        }

        // A world that ended halfway through its load leaves the loading screen up, the menu takes it down
        private void Start()
        {
            loadingScreenOverlay = FindAnyObjectByType<LoadingScreenOverlay>();
            loadingScreenOverlay.HideScreen();
            networkStarter.JoinFailed += HandleJoinFailed;
        }

        private void OnDestroy()
        {
            networkStarter.JoinFailed -= HandleJoinFailed;
            Application.targetFrameRate = UnlimitedFrameRate;
        }

        private void ShowWarning(string title, string details)
        {
            pendingWarningTitle = title;
            pendingWarningDetails = details;
            ShowPendingWarning();
        }

        private void Update()
        {
            if (!IsBound) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

            if (dialog.IsOpen)
            {
                dialog.CloseUnlessBusy();
            }
            else
            {
                pageStack.PopPageUnlessLast();
            }
        }

        protected override bool TryBindElements(VisualElement root)
        {
            PortConverters.RegisterGlobalConverters();
            root.dataSource = Config.GetGameConfig();

            panelElement = root.Q<VisualElement>(PanelElementName);
            Label versionLabel = root.Q<Label>(VersionLabelName);

            if (panelElement == null || versionLabel == null)
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
                dialog = new MainMenuDialog(root);
                mainPageController = new MainPage(MountPage(mainPage));
                newGamePageController = new NewGamePage(MountPage(newGamePage));
                savesPageController = new SavesPage(MountPage(savesPage), dialog);
                clientPageController = new ClientPage(MountPage(clientPage));
                serverSettingsPageController = new ServerSettingsPage(MountPage(serverSettingsPage));
                clientSettingsPageController = new ClientSettingsPage(MountPage(clientSettingsPage));
                accountPageController = new AccountPage(MountPage(accountPage));
            }
            catch (InvalidOperationException e)
            {
                Log.Error($"{e.Message}, the menu stays dead");
                return false;
            }

            versionLabel.text = Application.version;

            mainPageController.NewGameClicked += OpenNewGamePage;
            mainPageController.LoadGameClicked += OpenSavesPage;
            mainPageController.ServerSettingsClicked += OpenServerSettingsPage;
            mainPageController.ClientSettingsClicked += OpenClientSettingsPage;
            mainPageController.JoinClicked += OpenClientPage;
            mainPageController.AccountClicked += OpenAccountPage;
            mainPageController.QuitClicked += QuitGame;
            newGamePageController.StartClicked += HostFreshWorld;
            newGamePageController.BackClicked += ReturnToPreviousPage;
            savesPageController.SaveToLoadChosen += HostWorld;
            savesPageController.BackClicked += ReturnToPreviousPage;
            clientPageController.ConnectClicked += JoinWorldByInvite;
            clientPageController.BackClicked += ReturnToPreviousPage;
            serverSettingsPageController.BackClicked += ReturnToPreviousPage;
            clientSettingsPageController.BackClicked += ReturnToPreviousPage;
            accountPageController.BackClicked += ReturnToPreviousPage;

            pageStack.PushPage(mainPageController);

            if (networkStarter.StartFailureToShow != null)
            {
                pendingWarningTitle = networkStarter.StartFailureToShow.Title;
                pendingWarningDetails = networkStarter.StartFailureToShow.Details;
                networkStarter.StartFailureToShow = null;
            }

            ShowPendingWarning();

            if (Config.GetGameConfig().Account == null) CreateAccountWithGeneratedKeys();

            Log.Info("Menu is up");
            return true;
        }

        private void ShowPendingWarning()
        {
            if (dialog == null || pendingWarningTitle == null) return;

            dialog.ShowNotice(pendingWarningTitle, pendingWarningDetails, "Понятно");
            pendingWarningTitle = null;
            pendingWarningDetails = null;
        }

        private async void CreateAccountWithGeneratedKeys()
        {
            dialog.ShowBusy("Создание аккаунта...", "Пожалуйста, подождите. Может занять несколько секунд");
            try
            {
                Account account = await Task.Run(() => Account.CreateWithGeneratedKeys());
                Config.GetGameConfig().Account = account;
                Config.SaveGameConfig();
                Log.Info("Account key generated");
            }
            catch (Exception e)
            {
                Log.Error($"Account key generation failed: {e.Message}");
            }

            dialog?.CloseBusy();
        }

        protected override void UnbindElements()
        {
            pageStack.ClearPages();
            panelElement?.Clear();

            dialog = null;
            mainPageController = null;
            newGamePageController = null;
            savesPageController = null;
            clientPageController = null;
            serverSettingsPageController = null;
            clientSettingsPageController = null;
            accountPageController = null;
            panelElement = null;
        }

        private VisualElement MountPage(VisualTreeAsset template)
        {
            TemplateContainer pageElement = template.Instantiate();
            pageElement.name = template.name;
            pageElement.AddToClassList(PageClass);
            panelElement.Add(pageElement);

            return pageElement;
        }

        private void OpenNewGamePage()
        {
            pageStack.PushPage(newGamePageController);
        }

        private void OpenSavesPage()
        {
            pageStack.PushPage(savesPageController);
        }

        private void OpenClientPage()
        {
            pageStack.PushPage(clientPageController);
        }

        private void OpenServerSettingsPage()
        {
            pageStack.PushPage(serverSettingsPageController);
        }

        private void OpenClientSettingsPage()
        {
            pageStack.PushPage(clientSettingsPageController);
        }

        private void OpenAccountPage()
        {
            pageStack.PushPage(accountPageController);
        }

        private void ReturnToPreviousPage()
        {
            pageStack.PopPageUnlessLast();
        }

        private void HostFreshWorld()
        {
            Log.Info("The player starts a fresh game");
            HostWorld(null);
        }

        private void HostWorld(string saveLocation)
        {
            if (saveLocation != null) Log.Info($"The player continues the game saved at {saveLocation}");
            loadingScreenOverlay.ShowStage(LoadingScreenStage.Server);
            NetworkStartFailure failure = networkStarter.HostWorld(saveLocation);
            if (failure == null) return;

            loadingScreenOverlay.HideScreen();
            ShowWarning(failure.Title, failure.Details);
        }

        private void JoinWorldByInvite()
        {
            Log.Info("The player joins a game of someone else");
            NetworkStartFailure failure = networkStarter.JoinWorldByInvite();
            if (failure != null)
            {
                ShowWarning(failure.Title, failure.Details);
                return;
            }

            loadingScreenOverlay.ShowStage(LoadingScreenStage.Connection, CancelConnecting);
        }

        private void CancelConnecting()
        {
            networkStarter.CancelJoin();
            loadingScreenOverlay.HideScreen();
        }

        private void HandleJoinFailed(NetworkStartFailure failure)
        {
            loadingScreenOverlay.HideScreen();
            ShowWarning(failure.Title, failure.Details);
        }

        private void QuitGame()
        {
            Log.Info("Leaving the game by the player's own hand");
            Application.Quit();
        }
    }
}
