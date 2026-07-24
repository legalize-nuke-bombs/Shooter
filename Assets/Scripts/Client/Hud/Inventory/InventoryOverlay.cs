using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Shooter.Client.Ui;
using Shooter.Client.Worlds;
using Shooter.Client.Worlds.Entities;
using Shooter.Client.Worlds.Entities.Players;
using Shooter.Server.Worlds.Entities.Parts.Inventory;
using Shooter.Server.Worlds.Items;

namespace Shooter.Client.Hud.Inventory
{
    public sealed class InventoryOverlay : Overlay
    {
        private static readonly Color FrameColor = new Color(0.02f, 0.03f, 0.05f, 0.92f);

        private readonly Font font;
        private readonly ClientWorld world;
        private readonly VisualElement frame = new VisualElement();

        public InventoryOverlay(Font font, ClientWorld world, PlayerRig rig) : base(rig)
        {
            this.font = font;
            this.world = world;

            frame.style.position = Position.Absolute;
            frame.style.left = Length.Percent(35);
            frame.style.top = Length.Percent(25);
            frame.style.width = Length.Percent(30);
            frame.style.paddingLeft = 16;
            frame.style.paddingRight = 16;
            frame.style.paddingTop = 12;
            frame.style.paddingBottom = 12;
            frame.style.backgroundColor = FrameColor;
            Add(frame);
        }

        public override Key Hotkey => Key.I;

        protected override void OnOpen()
        {
            Refresh();
        }

        protected override void OnTick(float dt)
        {
            if (!IsOpen) return;

            Refresh();
        }

        private void Refresh()
        {
            EntityView me = world.Me;
            InventoryState state = me?.Inventory;

            frame.Clear();
            frame.Add(Line("ИНВЕНТАРЬ"));

            if (state?.Stacks != null)
            {
                foreach (KeyValuePair<StackableItem, int> stack in state.Stacks)
                    frame.Add(Line(stack.Key + "   " + stack.Value));
            }

            if (state?.Unique != null)
            {
                foreach (UniqueItemState item in state.Unique.Values)
                    frame.Add(Line(item.GetType().Name + "   #" + item.Id));
            }
        }

        private TextLine Line(string text)
        {
            var line = new TextLine(font, 14);
            line.text = text;
            return line;
        }
    }
}
