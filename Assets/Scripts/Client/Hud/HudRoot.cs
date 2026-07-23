using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Shooter.Client.Aiming;
using Shooter.Client.Hud.Hands;
using Shooter.Client.Hud.Inventory;
using Shooter.Client.Hud.Sleeping;
using Shooter.Client.Hud.Talking;
using Shooter.Client.Ui;
using Shooter.Client.Worlds;
using Shooter.Logging;

namespace Shooter.Client.Hud
{
    public class HudRoot
    {
        private const string FontPath = "Fonts/PTSans-Regular";

        private readonly VisualElement root;
        private readonly InventoryOverlay inventory;

        public HudRoot(VisualElement root, ClientWorld world, Aim aim)
        {
            this.root = root;
            var font = Resources.Load<Font>(FontPath);

            var sleepSense = new SleepSense(world, aim);
            var talkSense = new TalkSense(aim);

            root.pickingMode = PickingMode.Ignore;
            root.Add(new HandsOverlay(world));
            root.Add(new HpBar(world));
            root.Add(new Crosshair());
            root.Add(new TargetNameLabel(font, aim));
            root.Add(new SleepOverlay(sleepSense));
            root.Add(new ClockLabel(font, world));
            root.Add(new SleepHintLabel(font, sleepSense));
            root.Add(new TalkHintLabel(font, talkSense));
            root.Add(new DeadScreen(font, world));

            inventory = new InventoryOverlay(font, world);
            root.Add(inventory);
        }

        public void Tick(float dt)
        {
            foreach (VisualElement child in root.Children())
                if (child is UiElement element)
                    element.Tick(dt);

            if (!Keyboard.current.iKey.wasPressedThisFrame) return;

            inventory.Toggle();
            Log.Info("Hud: I pressed, inventory toggled");
        }
    }
}
