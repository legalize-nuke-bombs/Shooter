using System;
using Shooter.Game.Core;
using Shooter.Game.Core.Saves;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.AI.Bt.CustomOrders
{
    public class BtCustomOrderQueue : MonoBehaviour, IDigestible, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        private BtCustomOrder order = null;

        public string ComponentKey => "BtCustomOrderQueue";
        private struct SaveData
        {
            public string Kind { get; set; }
            public SaveToken State { get; set; }
        }
        public object SaveObject()
        {
            if (order == null) return new SaveData();

            object state = order.SaveObject();

            return new SaveData
            {
                Kind = order.Kind,
                State = state == null ? default : SaveToken.From(state)
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.To<SaveData>();
            order = null;

            if (sd.Kind == null) return;

            BtCustomOrder loaded = BtCustomOrder.Create(sd.Kind);
            if (loaded == null)
            {
                Log.Warn($"Entity {name} lost its order: kind {sd.Kind} is unknown");
                return;
            }

            if (!sd.State.Empty) loaded.LoadObject(sd.State);
            order = loaded;
        }

        public BtCustomOrder Current => order;

        public void ForcePut(BtCustomOrder newOrder)
        {
            if (newOrder == null)
            {
                throw new ArgumentNullException(nameof(newOrder));
            }
            order = newOrder;
        }

        public bool TryPut(BtCustomOrder newOrder)
        {
            if (newOrder == null)
            {
                throw new ArgumentNullException(nameof(newOrder));
            }
            if (order == null)
            {
                order = newOrder;
                return true;
            }
            return false;
        }

        public void Clear()
        {
            order = null;
        }

        public string Digest(DigestionDetail detail)
        {
            if (detail == DigestionDetail.Brief || order == null)
            {
                return null;
            }
            return order.PromptDescription();
        }

        public DigestionPriority Priority => DigestionPriority.High;
    }
}
