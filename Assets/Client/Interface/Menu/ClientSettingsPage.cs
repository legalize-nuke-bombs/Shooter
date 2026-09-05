using System;
using Shooter.Configuring;
using Shooter.Logging;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class ClientSettingsPage : MenuPage
    {
        private const string BackButton = "back";
        private static readonly Journal Log = Logs.Here();

        public ClientSettingsPage(VisualElement root) : base(root)
        {
            Require<Button>(BackButton).clicked += () => Backing?.Invoke();
        }

        public event Action Backing;

        protected override void Closed()
        {
            Config.Save();

            ClientConfig client = Config.Read().Client;
            Log.Info(
                $"Client settings: master {client.Master:0.00}, music {client.Music:0.00}, ambience {client.Ambience:0.00}, sounds {client.Sounds:0.00}, vhs {client.Vhs:0.00}");
        }
    }
}
