using System;
using Shooter.Configuring;
using Shooter.Logging;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class NewGamePage : MenuPage
    {
        private const string StartButton = "start";
        private const string BackButton = "back";
        private static readonly Journal Log = Logs.Here();

        public NewGamePage(VisualElement root) : base(root)
        {
            Require<Button>(StartButton).clicked += () => Starting?.Invoke();
            Require<Button>(BackButton).clicked += () => Backing?.Invoke();
        }

        public event Action Starting;

        public event Action Backing;

        protected override void Closed()
        {
            Config.Save();

            GameConfig config = Config.Read();
            Log.Info($"Own game on port {config.Server.Port} under the name {config.Client.Name}");
        }
    }
}
