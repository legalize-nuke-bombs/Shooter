using System;
using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public class RadioOverlay : Overlay, IShooterUpdateable
    {
        private const string WindowElementName = "radio";
        private const string ContactScrollViewName = "radio-list";
        private const string EmptyLabelName = "radio-empty";
        private static readonly Journal Log = Logs.Here();

        private PlayerConversations playerConversations;
        private PlayerRadio playerRadio;
        private IReadOnlyList<RadioPartner> partners;
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
            if (playerConversations != null)
            {
                playerConversations.HistoryArrived += HandleHistoryArrived;
                playerConversations.LiveLineArrived += HandleLiveLineArrived;
            }

            partners = null;
            playerRadio = playerController.GetComponent<PlayerRadio>();
            if (playerRadio != null)
            {
                playerRadio.PartnersArrived += HandlePartnersArrived;
                playerRadio.RequestPartnersRpc();
            }

            windowElement.style.display = DisplayStyle.Flex;
            stale = true;
            Log.Info("The radio is open");
        }

        private void CloseWindow()
        {
            if (playerConversations != null)
            {
                playerConversations.HistoryArrived -= HandleHistoryArrived;
                playerConversations.LiveLineArrived -= HandleLiveLineArrived;
            }
            playerConversations = null;

            if (playerRadio != null) playerRadio.PartnersArrived -= HandlePartnersArrived;
            playerRadio = null;
            partners = null;

            if (windowElement != null) windowElement.style.display = DisplayStyle.None;
            contactScrollView?.Clear();
            Log.Info("The radio is closed");
        }

        private void HandlePartnersArrived(IReadOnlyList<RadioPartner> arrived)
        {
            partners = arrived;
            stale = true;
        }

        private void HandleHistoryArrived(long partnerId)
        {
            stale = true;
        }

        private void HandleLiveLineArrived(long partnerId, ConversationLine line)
        {
            stale = true;
        }

        private void FillContactList(PlayerController playerController)
        {
            stale = false;
            contactScrollView.Clear();
            emptyLabel.style.display = partners != null && partners.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (partners == null) return;

            List<(RadioPartner Partner, string Name, Contact Contact)> entries = partners
                .Select(partner => (Partner: partner, Name: NameMapper.GetShownNameById(partner.CharacterId), Contact: FindContact(partner.CharacterId)))
                .OrderBy(entry => entry.Partner.OutOfReach)
                .ThenByDescending(entry => entry.Contact?.LastLineTime ?? DateTime.MinValue)
                .ThenBy(entry => entry.Name)
                .ToList();

            foreach ((RadioPartner partner, string name, Contact contact) in entries)
            {
                long partnerId = partner.CharacterId;

                Button contactButton = new(() => playerController.OpenRadioTalk(partnerId)) { text = name };
                contactButton.AddToClassList("radio__item");
                if (contact != null && contact.UnreadLineCount > 0) contactButton.AddToClassList("radio__item--unread");
                if (partner.OutOfReach) contactButton.AddToClassList("radio__item--out-of-reach");

                contactScrollView.Add(contactButton);
            }
        }

        private Contact FindContact(long partnerId)
        {
            return playerConversations != null && playerConversations.Contacts.TryGetValue(partnerId, out Contact contact) ? contact : null;
        }
    }
}
