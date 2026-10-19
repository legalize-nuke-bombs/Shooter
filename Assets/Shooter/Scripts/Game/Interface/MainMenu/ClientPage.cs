using System;
using Shooter.Infrastructure;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class ClientPage : MainMenuPage
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
            Log.Info($"Joining under the name {client.Name}");
        }
    }
}
