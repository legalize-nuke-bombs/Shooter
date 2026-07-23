using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Shooter.Client.Hud.Hands;
using Shooter.Client.Hud.Inventory;
using Shooter.Client.Hud.Sleeping;
using Shooter.Client.Hud.Talking;
using Shooter.Client.Ui;
using Shooter.Client.Worlds;
using Shooter.Client.Worlds.Entities;
using Shooter.Client.Worlds.Entities.Players;
using Shooter.Logging;

namespace Shooter.Client.Hud
{
    public class HudRoot
    {
        private const string FontPath = "Fonts/PTSans-Regular";

        private readonly VisualElement root;
        private readonly InventoryOverlay inventory;
        private readonly TalkDialog dialog;
        private readonly TalkSense talkSense;

        public HudRoot(VisualElement root, ClientWorld world, PlayerRig rig)
        {
            this.root = root;
            var font = Resources.Load<Font>(FontPath);

            var sleepSense = new SleepSense(world, rig.Aim);
            talkSense = new TalkSense(rig.Aim);

            root.pickingMode = PickingMode.Ignore;
            root.Add(new HandsOverlay(world));
            root.Add(new HpBar(world));
            root.Add(new Crosshair());
            root.Add(new TargetNameLabel(font, rig.Aim));
            root.Add(new SleepOverlay(sleepSense));
            root.Add(new ClockLabel(font, world));
            root.Add(new SleepHintLabel(font, sleepSense));
            root.Add(new TalkHintLabel(font, talkSense));
            root.Add(new DeadScreen(font, world));

            inventory = new InventoryOverlay(font, world);
            root.Add(inventory);

            dialog = new TalkDialog(font, world, rig);
            root.Add(dialog);
        }

        public void Tick(float dt)
        {
            foreach (VisualElement child in root.Children())
                if (child is UiElement element)
                    element.Tick(dt);

            if (dialog.IsOpen) return;

            Keyboard keyboard = Keyboard.current;

            if (keyboard.iKey.wasPressedThisFrame)
            {
                inventory.Toggle();
                Log.Info("Hud: I pressed, inventory toggled");
            }

            if (keyboard.pKey.wasPressedThisFrame)
            {
                EntityView talker = talkSense.TargetTalker();
                if (talker != null) dialog.Show(talker);
            }
        }

        public bool HandleEscape()
        {
            return dialog.Hide();
        }
    }
}
