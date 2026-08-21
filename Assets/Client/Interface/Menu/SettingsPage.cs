using System;
using Shooter.Configuring;
using Shooter.Logging;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class SettingsPage : MenuPage
    {
        private const string ClientTab = "tab-client";
        private const string ServerTab = "tab-server";
        private const string ClientElement = "client";
        private const string ServerElement = "server";
        private const string BackButton = "back";
        private const string ChosenClass = "tab--chosen";
        private static readonly Journal Log = Logs.Here();

        private readonly VisualElement client;
        private readonly Button clientTab;
        private readonly VisualElement server;
        private readonly Button serverTab;

        public SettingsPage(VisualElement root) : base(root)
        {
            client = Require<VisualElement>(ClientElement);
            server = Require<VisualElement>(ServerElement);
            clientTab = Require<Button>(ClientTab);
            serverTab = Require<Button>(ServerTab);

            clientTab.clicked += () => Choose(client);
            serverTab.clicked += () => Choose(server);
            Require<Button>(BackButton).clicked += () => Backing?.Invoke();

            Choose(client);
        }

        public event Action Backing;

        protected override void Closed()
        {
            Config.Save();
            Log.Info("Settings kept");
        }

        private void Choose(VisualElement tab)
        {
            client.style.display = tab == client ? DisplayStyle.Flex : DisplayStyle.None;
            server.style.display = tab == server ? DisplayStyle.Flex : DisplayStyle.None;

            clientTab.EnableInClassList(ChosenClass, tab == client);
            serverTab.EnableInClassList(ChosenClass, tab == server);
        }
    }
}
