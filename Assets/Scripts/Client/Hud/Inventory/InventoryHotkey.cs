using UnityEngine;
using UnityEngine.InputSystem;
using Shooter.Logging;

namespace Shooter.Client.Hud.Inventory
{
    public class InventoryHotkey : MonoBehaviour
    {
        private InventoryOverlay overlay;

        public void Bind(InventoryOverlay overlay)
        {
            this.overlay = overlay;
        }

        private void Awake()
        {
            if (Application.isBatchMode) enabled = false;
        }

        private void Update()
        {
            if (!Keyboard.current.iKey.wasPressedThisFrame) return;
            overlay.Toggle();
            Log.Info("Inventory: I pressed, panel toggled");
        }
    }
}
