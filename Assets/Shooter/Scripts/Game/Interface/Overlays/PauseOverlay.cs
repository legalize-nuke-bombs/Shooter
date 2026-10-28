using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public class PauseOverlay : Overlay, IShooterUpdateable
    {
        private const string WindowElementName = "pause";
        private const string ResumeButtonName = "pause-resume";
        private const string SaveButtonName = "pause-save";
        private const string InviteButtonName = "pause-invite";
        private const string LeaveButtonName = "pause-leave";
        private const string LeaveTextForHost = "Сохранить и выйти";
        private const string LeaveTextForClient = "Покинуть мир";
        private static readonly Journal Log = Logs.Here();

        private Button inviteButton;
        private Button leaveButton;
        private bool open;
        private Button resumeButton;
        private Button saveButton;
        private VisualElement windowElement;

        public void ShooterUpdate()
        {
            if (!IsBound) return;

            PlayerController playerController = LocalPlayerFinder.FindComponent<PlayerController>();
            bool shouldBeOpen = playerController != null && playerController.IsPauseMenuOpen && !playerController.IsInviteOpen;

            if (shouldBeOpen == open) return;

            open = shouldBeOpen;

            if (open)
            {
                ShowWindow(playerController);
            }
            else
            {
                HideWindow();
            }
        }

        protected override bool TryBindElements(VisualElement root)
        {
            windowElement = root.Q<VisualElement>(WindowElementName);
            resumeButton = root.Q<Button>(ResumeButtonName);
            saveButton = root.Q<Button>(SaveButtonName);
            inviteButton = root.Q<Button>(InviteButtonName);
            leaveButton = root.Q<Button>(LeaveButtonName);

            if (windowElement == null || resumeButton == null || saveButton == null || inviteButton == null || leaveButton == null)
            {
                Log.Error($"Overlay document has no {WindowElementName} window, the pause menu stays hidden");
                return false;
            }

            resumeButton.clicked += ClosePauseMenu;
            saveButton.clicked += SaveWorld;
            inviteButton.clicked += OpenInvite;
            leaveButton.clicked += LeaveWorld;
            windowElement.style.display = DisplayStyle.None;

            return true;
        }

        protected override void UnbindElements()
        {
            open = false;
            windowElement = null;
        }

        private void ShowWindow(PlayerController playerController)
        {
            bool isHost = playerController.IsServer;

            saveButton.style.display = isHost ? DisplayStyle.Flex : DisplayStyle.None;
            inviteButton.style.display = isHost ? DisplayStyle.Flex : DisplayStyle.None;
            leaveButton.text = isHost ? LeaveTextForHost : LeaveTextForClient;

            windowElement.style.display = DisplayStyle.Flex;
        }

        private void HideWindow()
        {
            if (windowElement != null) windowElement.style.display = DisplayStyle.None;
        }

        private void ClosePauseMenu()
        {
            LocalPlayerFinder.FindComponent<PlayerController>()?.ClosePauseMenu();
        }

        private void SaveWorld()
        {
            LocalPlayerFinder.FindComponent<PlayerController>()?.SaveWorld();
        }

        private void OpenInvite()
        {
            LocalPlayerFinder.FindComponent<PlayerController>()?.OpenInvite();
        }

        private void LeaveWorld()
        {
            LocalPlayerFinder.FindComponent<PlayerController>()?.LeaveWorld();
        }
    }
}
