using System;
using Shooter.Infrastructure;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class ClientSettingsPage : MainMenuPage
    {
        private const string BackButton = "back";
        private const string AntialiasingField = "antialiasing";
        private const string UpscalerField = "upscaler";
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        public ClientSettingsPage(VisualElement root) : base(root)
        {
            Require<Button>(BackButton).clicked += () => Backing?.Invoke();
            antialiasing = Require<DropdownField>(AntialiasingField);
            Offer(antialiasing, Antialiasings.Keys, Titles.Antialiasing);
            Offer(Require<DropdownField>(UpscalerField), Upscalers.CheapestToBest, Titles.Upscaler);
        }

        private readonly DropdownField antialiasing;
        private ClientConfig client;

        protected override void Opened()
        {
            client = Config.GetGameConfig().Client;
            client.propertyChanged += Changed;
            Follow();
        }

        private void Changed(object sender, BindablePropertyChangedEventArgs args)
        {
            Follow();
        }

        // DLSS replaces the camera antialiasing, so that row is greyed out while it is on
        private void Follow()
        {
            antialiasing.SetEnabled(client.Upscaler == Upscalers.Off);
        }

        public event Action Backing;

        protected override void Closed()
        {
            if (client != null) client.propertyChanged -= Changed;
            Config.SaveGameConfig();

            ClientConfig saved = Config.GetGameConfig().Client;
            Log.Info(
                $"Client settings: master {saved.Master:0.00}, music {saved.Music:0.00}, ambience {saved.Ambience:0.00}, sounds {saved.Sounds:0.00}, antialiasing {saved.Antialiasing}, upscaler {saved.Upscaler}");
        }
    }
}
