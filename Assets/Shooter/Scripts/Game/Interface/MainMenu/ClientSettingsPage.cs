using System;
using Shooter.Infrastructure;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class ClientSettingsPage : MainMenuPage
    {
        private const string BackButtonName = "back";
        private const string AntialiasingDropdownName = "antialiasing";
        private const string UpscalerDropdownName = "upscaler";
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private readonly DropdownField antialiasingDropdown;
        private ClientConfig clientConfig;

        public ClientSettingsPage(VisualElement root) : base(root)
        {
            RequireElement<Button>(BackButtonName).clicked += () => BackClicked?.Invoke();
            antialiasingDropdown = RequireElement<DropdownField>(AntialiasingDropdownName);
            SetDropdownChoices(antialiasingDropdown, Antialiasings.Keys, SettingsDropdownTitles.GetAntialiasingTitle);
            SetDropdownChoices(RequireElement<DropdownField>(UpscalerDropdownName), Upscalers.CheapestToBest, SettingsDropdownTitles.GetUpscalerTitle);
        }

        public event Action BackClicked;

        protected override void HandlePageShown()
        {
            clientConfig = Config.GetGameConfig().Client;
            clientConfig.propertyChanged += HandleClientConfigChanged;
            EnableAntialiasingDropdownIfUpscalerOff();
        }

        private void HandleClientConfigChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            EnableAntialiasingDropdownIfUpscalerOff();
        }

        // DLSS replaces the camera antialiasing, so that row is greyed out while it is on
        private void EnableAntialiasingDropdownIfUpscalerOff()
        {
            antialiasingDropdown.SetEnabled(clientConfig.Upscaler == Upscalers.Off);
        }

        protected override void HandlePageHidden()
        {
            if (clientConfig != null) clientConfig.propertyChanged -= HandleClientConfigChanged;
            Config.SaveGameConfig();

            ClientConfig savedClientConfig = Config.GetGameConfig().Client;
            Log.Info($"Client settings: master {savedClientConfig.Master:0.00}, music {savedClientConfig.Music:0.00}, ambience {savedClientConfig.Ambience:0.00}, sounds {savedClientConfig.Sounds:0.00}, antialiasing {savedClientConfig.Antialiasing}, upscaler {savedClientConfig.Upscaler}");
        }
    }
}
