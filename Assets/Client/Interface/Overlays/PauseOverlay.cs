using Shooter.Client.Playing;
using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class PauseOverlay : Overlay
    {
        private const string WindowElement = "pause";
        private const string ResumeElement = "pause-resume";
        private const string SaveElement = "pause-save";
        private const string InviteElement = "pause-invite";
        private const string LeaveElement = "pause-leave";
        private const string LeaveHost = "Сохранить и выйти";
        private const string LeaveClient = "Покинуть мир";
        private static readonly Journal Log = Logs.Here();

        private Button invite;
        private Button leave;
        private bool open;
        private Button resume;
        private Button save;
        private VisualElement window;

        private void Update()
        {
            if (!Bound) return;

            LocalPlayer player = OwnPlayer.Find<LocalPlayer>();
            bool wanted = player != null && player.Paused && !player.Inviting;

            if (wanted == open) return;

            open = wanted;

            if (open) Show(player);
            else Hide();
        }

        protected override bool Bind(VisualElement root)
        {
            window = root.Q<VisualElement>(WindowElement);
            resume = root.Q<Button>(ResumeElement);
            save = root.Q<Button>(SaveElement);
            invite = root.Q<Button>(InviteElement);
            leave = root.Q<Button>(LeaveElement);

            if (window == null || resume == null || save == null || invite == null || leave == null)
            {
                Log.Error($"Overlay document has no {WindowElement} window, the pause menu stays hidden");
                return false;
            }

            resume.clicked += Resume;
            save.clicked += Save;
            invite.clicked += Invite;
            leave.clicked += Leave;
            window.style.display = DisplayStyle.None;

            return true;
        }

        protected override void Unbind()
        {
            open = false;
            window = null;
        }

        private void Show(LocalPlayer player)
        {
            bool host = player.IsServer;

            save.style.display = host ? DisplayStyle.Flex : DisplayStyle.None;
            invite.style.display = host ? DisplayStyle.Flex : DisplayStyle.None;
            leave.text = host ? LeaveHost : LeaveClient;

            window.style.display = DisplayStyle.Flex;
        }

        private void Hide()
        {
            if (window != null) window.style.display = DisplayStyle.None;
        }

        private void Resume()
        {
            OwnPlayer.Find<LocalPlayer>()?.Resume();
        }

        private void Save()
        {
            OwnPlayer.Find<LocalPlayer>()?.SaveWorld();
        }

        private void Invite()
        {
            OwnPlayer.Find<LocalPlayer>()?.OpenInvite();
        }

        private void Leave()
        {
            OwnPlayer.Find<LocalPlayer>()?.LeaveWorld();
        }
    }
}
