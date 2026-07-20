using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Ui;
using Shooter.Client.Worlds;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities;
using Shooter.Server.Worlds.Entities.Parts.Inventory;
using Shooter.Server.Worlds.Items;
using Shooter.Server.Worlds.Items.Firearm;

namespace Shooter.Client.Hud.Hands
{
    public class HandsOverlay : Overlay
    {
        private const long RefreshMs = 16;

        private readonly ClientWorld world;

        public HandsOverlay(ClientWorld world)
        {
            this.world = world;
            Animate(RefreshMs);
        }
        protected override void Draw(Painter2D painter, Rect rect)
        {
            EntityState me = world.Me;
            if (me == null)
            {
                return;
            }

            InventoryState inventoryState = me.Part<InventoryState>();
            if (inventoryState == null || inventoryState.EquiptedId == null)
            {
                return;
            }

            UniqueItemState equipted = inventoryState.Unique.GetValueOrDefault(inventoryState.EquiptedId.Value, null);
            if (equipted != null && equipted is FirearmState firearmState)
            {
                switch (firearmState.FirearmType)
                {
                    case FirearmType.Ak47:
                        painter.strokeColor = new Color(0, 0, 0);
                        painter.lineWidth = 50;
                        painter.BeginPath();
                        painter.MoveTo(new Vector2(rect.width * 0.9f, rect.height * 0.9f));
                        painter.LineTo(new Vector2(rect.width * 1.0f, rect.height * 1.0f));
                        painter.Stroke();
                        break;
                    default:
                        Log.Warn("Unexpected FirearmType {}", firearmState.FirearmType);
                        break;
                }
            }
        }
    }
}
