using System;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Core.Saves;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.AI.Bt.CustomOrders
{
    [RequireComponent(typeof(AgentMovement))]
    [RequireComponent(typeof(BtReports))]
    public class BtCustomOrderQueue : MonoBehaviour, IDigestible, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        private BtCustomOrder order = null;
        private AgentMovement movement;
        private BtReports reports;

        private void Awake()
        {
            movement = GetComponent<AgentMovement>();
            reports = GetComponent<BtReports>();
        }

        private void Update()
        {
            Settle();
        }

        private void Settle()
        {
            if (!movement.IsSpawned || !movement.IsServer) return;
            if (order == null || !order.Done(gameObject)) return;

            Finish();
        }

        public void Finish()
        {
            if (order == null)
            {
                throw new InvalidOperationException($"Entity {name} has no custom order to finish");
            }

            string outcome = order.PromptOutcome(gameObject);
            Log.Info($"Entity {name} finished {order.Kind} custom order: {outcome}");
            order = null;
            reports.Report(new BtReport { Prompt = outcome, Urgent = true });
        }

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
            Settle();
            order = newOrder;
        }

        public bool TryPut(BtCustomOrder newOrder)
        {
            if (newOrder == null)
            {
                throw new ArgumentNullException(nameof(newOrder));
            }
            Settle();
            if (order == null)
            {
                order = newOrder;
                return true;
            }
            return false;
        }

        public void Clear()
        {
            Settle();
            order = null;
        }

        public string Digest(DigestionDetail detail)
        {
            if (detail == DigestionDetail.Brief)
            {
                return null;
            }

            if (order == null)
            {
                return "No active second-level behavior tree actions";
            }
            return "Second-level behaviour tree action: " + order.PromptDescription(gameObject);
        }

        public DigestionPriority Priority => DigestionPriority.High;
    }
}
