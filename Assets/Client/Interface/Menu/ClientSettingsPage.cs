using System;
using Shooter.Configuring;
using Shooter.Logging;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class ClientSettingsPage : MenuPage
    {
        private const string BackButton = "back";
        private const string AntialiasingField = "antialiasing";
        private const string UpscalerField = "upscaler";
        private static readonly Journal Log = Logs.Here();

        public ClientSettingsPage(VisualElement root) : base(root)
        {
            Require<Button>(BackButton).clicked += () => Backing?.Invoke();
            antialiasing = Require<DropdownField>(AntialiasingField);
            Offer(antialiasing, Antialiasings.Keys, Titles.Antialiasing);
            Offer(Require<DropdownField>(UpscalerField), Upscalers.Keys, Titles.Upscaler);
        }

        private readonly DropdownField antialiasing;
        private ClientConfig client;

        protected override void Opened()
        {
            client = Config.Read().Client;
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
            Config.Save();

            ClientConfig saved = Config.Read().Client;
            Log.Info(
                $"Client settings: master {saved.Master:0.00}, music {saved.Music:0.00}, ambience {saved.Ambience:0.00}, sounds {saved.Sounds:0.00}, antialiasing {saved.Antialiasing}, upscaler {saved.Upscaler}");
        }
    }
}
