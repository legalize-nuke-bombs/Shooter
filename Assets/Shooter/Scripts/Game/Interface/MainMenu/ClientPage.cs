using System;
using Shooter.Infrastructure;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class ClientPage : MainMenuPage
    {
        private const string ConnectButtonName = "connect";
        private const string BackButtonName = "back";
        private static readonly Journal Log = Logs.Here();

        public ClientPage(VisualElement root) : base(root)
        {
            RequireElement<Button>(ConnectButtonName).clicked += () => ConnectClicked?.Invoke();
            RequireElement<Button>(BackButtonName).clicked += () => BackClicked?.Invoke();
        }

        public event Action ConnectClicked;

        public event Action BackClicked;

        protected override void HandlePageHidden()
        {
            Config.SaveGameConfig();

            ClientConfig clientConfig = Config.GetGameConfig().Client;
            Log.Info($"Joining under the name {clientConfig.Name}");
        }
    }
}
