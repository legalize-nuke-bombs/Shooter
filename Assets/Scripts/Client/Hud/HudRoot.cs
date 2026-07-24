using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Shooter.Client.Hud.Hands;
using Shooter.Client.Hud.Inventory;
using Shooter.Client.Hud.Sleeping;
using Shooter.Client.Hud.Talking;
using Shooter.Client.Ui;
using Shooter.Client.Worlds;
using Shooter.Client.Worlds.Entities.Players;

namespace Shooter.Client.Hud
{
    public sealed class HudRoot : UiElement
    {
        private const string FontPath = "Fonts/PTSans-Regular";

        private readonly Overlay[] overlays;

        public HudRoot(ClientWorld world, PlayerRig rig)
        {
            var font = Resources.Load<Font>(FontPath);
            pickingMode = PickingMode.Ignore;
            Fullscreen();

            var sleepSense = new SleepSense(world, rig.Aim);
            var talkSense = new TalkSense(rig.Aim);

            Add(new HandsOverlay(world));
            Add(new HpBar(world));
            Add(new Crosshair());
            Add(new TargetNameLabel(font, rig.Aim));
            Add(new SleepOverlay(sleepSense));
            Add(new ClockLabel(font, world));
            Add(new SleepHintLabel(font, sleepSense));
            Add(new TalkHintLabel(font, talkSense));
            Add(new DeadScreen(font, world));

            var inventory = new InventoryOverlay(font, world, rig);
            var dialog = new TalkDialog(font, world, rig, talkSense);
            overlays = new Overlay[] { inventory, dialog };

            foreach (Overlay overlay in overlays)
                Add(overlay);
        }

        public bool Escape()
        {
            foreach (Overlay overlay in overlays)
                if (overlay.Close()) return true;

            return false;
        }

        protected override void OnTick(float dt)
        {
            Keyboard keyboard = Keyboard.current;

            foreach (Overlay overlay in overlays)
            {
                if (!keyboard[overlay.Hotkey].wasPressedThisFrame) continue;

                if (overlay.IsOpen)
                {
                    overlay.Close();
                    return;
                }

                CloseAll();
                overlay.Open();
                return;
            }
        }

        private void CloseAll()
        {
            foreach (Overlay overlay in overlays)
                overlay.Close();
        }
    }
}
