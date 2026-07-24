using UnityEngine;
using UnityEngine.InputSystem;
using Shooter.Client.Worlds.Entities.Players;
using Shooter.Logging;

namespace Shooter.Client.Ui
{
    public abstract class Overlay : UiElement
    {
        private readonly PlayerRig rig;

        protected Overlay(PlayerRig rig)
        {
            this.rig = rig;
            Fullscreen();
            Visible = false;
        }

        public bool IsOpen { get; private set; }

        public abstract Key Hotkey { get; }

        public bool TryOpen()
        {
            if (IsOpen || !CanOpen()) return false;

            IsOpen = true;
            Visible = true;
            rig.UiCaptured = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            OnOpen();
            Log.Info("Overlay {} opened", GetType().Name);
            return true;
        }

        public bool Close()
        {
            if (!IsOpen) return false;

            IsOpen = false;
            Visible = false;
            rig.UiCaptured = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            OnClose();
            Log.Info("Overlay {} closed", GetType().Name);
            return true;
        }

        protected virtual bool CanOpen()
        {
            return true;
        }

        protected virtual void OnOpen()
        {
        }

        protected virtual void OnClose()
        {
        }
    }
}
