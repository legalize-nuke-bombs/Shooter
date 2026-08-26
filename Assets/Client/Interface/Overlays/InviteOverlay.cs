using Shooter.Client.Playing;
using Shooter.Configuring;
using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class InviteOverlay : Overlay
    {
        private const string WindowElement = "invite";
        private const string AddressElement = "invite-address";
        private const string PortElement = "invite-port";
        private const string CopyElement = "invite-copy";
        private const string BackElement = "invite-back";
        private const string StatusElement = "invite-status";
        private const string Copied = "Ссылка скопирована";
        private const string NoAddress = "Впишите адрес";
        private static readonly Journal Log = Logs.Here();

        private TextField address;
        private Button back;
        private Button copy;
        private bool open;
        private TextField port;
        private Label status;
        private VisualElement window;

        private void Update()
        {
            if (!Bound) return;

            LocalPlayer player = OwnPlayer.Find<LocalPlayer>();
            bool wanted = player != null && player.Inviting;

            if (wanted == open) return;

            open = wanted;

            if (open) Show();
            else Hide();
        }

        protected override bool Bind(VisualElement root)
        {
            window = root.Q<VisualElement>(WindowElement);
            address = root.Q<TextField>(AddressElement);
            port = root.Q<TextField>(PortElement);
            copy = root.Q<Button>(CopyElement);
            back = root.Q<Button>(BackElement);
            status = root.Q<Label>(StatusElement);

            if (window == null || address == null || port == null || copy == null || back == null || status == null)
            {
                Log.Error($"Overlay document has no {WindowElement} window, invites stay hidden");
                return false;
            }

            copy.clicked += Copy;
            back.clicked += Back;
            window.style.display = DisplayStyle.None;

            return true;
        }

        protected override void Unbind()
        {
            open = false;
            window = null;
        }

        private void Show()
        {
            port.value = Config.Read().Server.Port.ToString();
            status.text = "";
            window.style.display = DisplayStyle.Flex;
        }

        private void Hide()
        {
            if (window != null) window.style.display = DisplayStyle.None;
        }

        private void Copy()
        {
            string host = address.value.Trim();
            if (string.IsNullOrEmpty(host))
            {
                status.text = NoAddress;
                return;
            }

            if (!ushort.TryParse(port.value, out ushort chosen))
                chosen = Config.Read().Server.Port;

            string code = Invite.Encode(host, chosen, Config.Account.Certificate);
            GUIUtility.systemCopyBuffer = code;
            status.text = Copied;
            Log.Info($"Invite link copied for {host}:{chosen}");
        }

        private void Back()
        {
            OwnPlayer.Find<LocalPlayer>()?.CloseInvite();
        }
    }
}
