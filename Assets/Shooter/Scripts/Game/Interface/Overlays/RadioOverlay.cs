using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class RadioOverlay : Overlay
    {
        private const string WindowElementName = "radio";
        private const string ContactScrollViewName = "radio-list";
        private const string EmptyLabelName = "radio-empty";
        private const string Stranger = "Незнакомец";
        private static readonly Journal Log = Logs.Here();

        private PlayerConversations playerConversations;
        private Label emptyLabel;
        private ScrollView contactScrollView;
        private bool open;
        private bool stale;
        private VisualElement windowElement;

        private void Update()
        {
            if (!IsBound) return;

            LocalPlayer localPlayer = OwnPlayer.FindComponent<LocalPlayer>();
            bool shouldBeOpen = localPlayer != null && localPlayer.IsRadioOpen;

            if (shouldBeOpen != open)
            {
                open = shouldBeOpen;

                if (open)
                {
                    OpenWindow(localPlayer);
                }
                else
                {
                    CloseWindow();
                }
            }

            if (open && stale) FillContactList(localPlayer);
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

        private void OpenWindow(LocalPlayer localPlayer)
        {
            playerConversations = localPlayer.GetComponent<PlayerConversations>();
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

        private void FillContactList(LocalPlayer localPlayer)
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

                Button contactButton = new(() => localPlayer.OpenRadioTalk(partnerId)) { text = GetPartnerName(partnerId) };
                contactButton.AddToClassList("radio__item");
                if (contact.UnreadLineCount > 0) contactButton.AddToClassList("radio__item--unread");

                contactScrollView.Add(contactButton);
            }
        }

        private string GetPartnerName(long partnerId)
        {
            Character partner = Character.FindById(partnerId, RegisterInactivePolicy.Exclude);
            Nameable nameable = partner == null ? null : partner.GetComponentInChildren<Nameable>();
            if (nameable == null) return Stranger;

            string shownName = nameable.GetShownName();

            return string.IsNullOrEmpty(shownName) ? Stranger : shownName;
        }
    }
}
