using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Shooter.Client.Aiming;
using Shooter.Client.Hud.Inventory;
using Shooter.Client.Hud.Sleeping;
using Shooter.Client.Worlds;
using Shooter.Logging;

namespace Shooter.Client.Hud
{
    public class HudRoot
    {
        private const string FontPath = "Fonts/PTSans-Regular";

        private readonly InventoryOverlay inventory;

        public HudRoot(VisualElement root, ClientWorld world, Aim aim)
        {
            var font = Resources.Load<Font>(FontPath);
            var sleepSense = new SleepSense(world, aim);

            root.pickingMode = PickingMode.Ignore;
            root.Add(new HpBar(world));
            root.Add(new Crosshair());
            root.Add(new TargetNameLabel(font, aim));
            root.Add(new SleepOverlay(sleepSense));
            root.Add(new ClockLabel(font, world));
            root.Add(new SleepHintLabel(font, sleepSense));

            inventory = new InventoryOverlay(font, world);
            root.Add(inventory);
        }

        public void Tick()
        {
            if (!Keyboard.current.iKey.wasPressedThisFrame) return;

            inventory.Toggle();
            Log.Info("Hud: I pressed, inventory toggled");
        }
    }
}
