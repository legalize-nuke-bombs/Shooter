using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Ui;
using Shooter.Client.Worlds;
using Shooter.Server.Worlds.Entities;
using Shooter.Server.Worlds.Entities.Parts.Inventory;
using Shooter.Server.Worlds.Items;

namespace Shooter.Client.Hud.Inventory
{
    public class InventoryOverlay : UiElement
    {
        private static readonly Color FrameColor = new Color(0.02f, 0.03f, 0.05f, 0.92f);

        private readonly Font font;
        private readonly ClientWorld world;
        private readonly VisualElement frame = new VisualElement();

        private bool open;

        public InventoryOverlay(Font font, ClientWorld world)
        {
            this.font = font;
            this.world = world;

            Fullscreen();
            Visible = false;

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

        public void Toggle()
        {
            open = !open;
            Visible = open;
            if (open) Refresh();
        }

        protected override void OnTick(float dt)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (!open) return;

            EntityState me = world.Me;
            InventoryState state = me?.Part<InventoryState>();

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

        private Label Line(string text)
        {
            var line = new TextLine(font, 14);
            line.text = text;
            return line;
        }
    }
}
