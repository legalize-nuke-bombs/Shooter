using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class RadioOverlay : Overlay, IShooterUpdateable
    {
        private const string WindowElementName = "radio";
        private const string ContactScrollViewName = "radio-list";
        private const string EmptyLabelName = "radio-empty";
        private static readonly Journal Log = Logs.Here();

        private PlayerConversations playerConversations;
        private Label emptyLabel;
        private ScrollView contactScrollView;
        private bool open;
        private bool stale;
        private VisualElement windowElement;

        public void ShooterUpdate()
        {
            if (!IsBound) return;

            PlayerController playerController = LocalPlayerFinder.FindComponent<PlayerController>();
            bool shouldBeOpen = playerController != null && playerController.IsRadioOpen;

            if (shouldBeOpen != open)
            {
                open = shouldBeOpen;

                if (open)
                {
                    OpenWindow(playerController);
                }
                else
                {
                    CloseWindow();
                }
            }

            if (open && stale) FillContactList(playerController);
        }

        protected override bool TryBindElements(VisualElement root)
        {
            windowElement = root.Q<VisualElement>(WindowElementName);
            contactScrollView = root.Q<ScrollView>(ContactScrollViewName);
            emptyLabel = root.Q<Label>(EmptyLabelName);

            if (windowElement == null || contactScrollView == null || emptyLabel == null)
            {
                Log.Error($"Overlay document has no {WindowElementName} window, the radio stays hidden");
                return false;
            }

            windowElement.style.display = DisplayStyle.None;

            return true;
        }

        protected override void UnbindElements()
        {
            if (open) CloseWindow();

            open = false;
            windowElement = null;
        }

        private void OpenWindow(PlayerController playerController)
        {
            playerConversations = playerController.GetComponent<PlayerConversations>();
            if (playerConversations != null) playerConversations.LinesArrived += HandleLinesArrived;

            windowElement.style.display = DisplayStyle.Flex;
            stale = true;
            Log.Info("The radio is open");
        }

        private void CloseWindow()
        {
            if (playerConversations != null) playerConversations.LinesArrived -= HandleLinesArrived;
            playerConversations = null;

            if (windowElement != null) windowElement.style.display = DisplayStyle.None;
            contactScrollView?.Clear();
            Log.Info("The radio is closed");
        }

        private void HandleLinesArrived(long partnerId)
        {
            stale = true;
        }

        private void FillContactList(PlayerController playerController)
        {
            stale = false;
            contactScrollView.Clear();

            List<Contact> contacts = playerConversations == null
                ? new List<Contact>()
                : playerConversations.Contacts.Values.OrderByDescending(contact => contact.LastLineTime).ToList();

            emptyLabel.style.display = contacts.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

            foreach (Contact contact in contacts)
            {
                long partnerId = contact.PartnerId;

                Button contactButton = new(() => playerController.OpenRadioTalk(partnerId)) { text = NameMapper.GetShownNameById(partnerId) };
                contactButton.AddToClassList("radio__item");
                if (contact.UnreadLineCount > 0) contactButton.AddToClassList("radio__item--unread");

                contactScrollView.Add(contactButton);
            }
        }
    }
}
