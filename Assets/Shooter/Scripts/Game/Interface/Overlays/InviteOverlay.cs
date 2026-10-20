using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class InviteOverlay : Overlay
    {
        private const string WindowElementName = "invite";
        private const string AddressFieldName = "invite-address";
        private const string PortFieldName = "invite-port";
        private const string CopyButtonName = "invite-copy";
        private const string BackButtonName = "invite-back";
        private const string StatusLabelName = "invite-status";
        private const string CopiedStatus = "Ссылка скопирована";
        private const string NoAddressStatus = "Впишите адрес";
        private static readonly Journal Log = Logs.Here();

        private TextField addressField;
        private Button backButton;
        private Button copyButton;
        private bool open;
        private TextField portField;
        private Label statusLabel;
        private VisualElement windowElement;

        private void Update()
        {
            if (!IsBound) return;

            PlayerController playerController = LocalPlayerFinder.FindComponent<PlayerController>();
            bool shouldBeOpen = playerController != null && playerController.IsInviteOpen;

            if (shouldBeOpen == open) return;

            open = shouldBeOpen;

            if (open)
            {
                ShowWindow();
            }
            else
            {
                HideWindow();
            }
        }

        protected override bool TryBindElements(VisualElement root)
        {
            windowElement = root.Q<VisualElement>(WindowElementName);
            addressField = root.Q<TextField>(AddressFieldName);
            portField = root.Q<TextField>(PortFieldName);
            copyButton = root.Q<Button>(CopyButtonName);
            backButton = root.Q<Button>(BackButtonName);
            statusLabel = root.Q<Label>(StatusLabelName);

            if (windowElement == null || addressField == null || portField == null || copyButton == null || backButton == null || statusLabel == null)
            {
                Log.Error($"Overlay document has no {WindowElementName} window, invites stay hidden");
                return false;
            }

            copyButton.clicked += CopyInviteLink;
            backButton.clicked += CloseInvite;
            windowElement.style.display = DisplayStyle.None;

            return true;
        }

        protected override void UnbindElements()
        {
            open = false;
            windowElement = null;
        }

        private void ShowWindow()
        {
            portField.value = Config.GetGameConfig().Server.Port.ToString();
            statusLabel.text = "";
            windowElement.style.display = DisplayStyle.Flex;
        }

        private void HideWindow()
        {
            if (windowElement != null) windowElement.style.display = DisplayStyle.None;
        }

        private void CopyInviteLink()
        {
            string hostAddress = addressField.value.Trim();
            if (string.IsNullOrEmpty(hostAddress))
            {
                statusLabel.text = NoAddressStatus;
                return;
            }

            if (!ushort.TryParse(portField.value, out ushort port))
            {
                port = Config.GetGameConfig().Server.Port;
            }

            string inviteCode = NetworkInvite.EncodeInviteCode(hostAddress, port, Config.Account.Certificate);
            GUIUtility.systemCopyBuffer = inviteCode;
            statusLabel.text = CopiedStatus;
            Log.Info($"Invite link copied for {hostAddress}:{port}");
        }

        private void CloseInvite()
        {
            LocalPlayerFinder.FindComponent<PlayerController>()?.CloseInvite();
        }
    }
}
