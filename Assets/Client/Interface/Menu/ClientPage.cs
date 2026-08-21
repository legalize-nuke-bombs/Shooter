using System;
using Shooter.Configuring;
using Shooter.Logging;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class ClientPage : MenuPage
    {
        private const string ConnectButton = "connect";
        private const string BackButton = "back";
        private static readonly Journal Log = Logs.Here();

        public ClientPage(VisualElement root) : base(root)
        {
            Require<Button>(ConnectButton).clicked += () => Connecting?.Invoke();
            Require<Button>(BackButton).clicked += () => Backing?.Invoke();
        }

        public event Action Connecting;

        public event Action Backing;

        protected override void Closed()
        {
            Config.Save();

            ClientConfig client = Config.Read().Client;
            Log.Info($"World of {client.Address}:{client.Port} under the name {client.Name}");
        }
    }
}
