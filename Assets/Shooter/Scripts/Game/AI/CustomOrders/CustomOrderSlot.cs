using System;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(BehaviorReporter))]
    public class CustomOrderSlot : MonoBehaviour, IDigestible, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        private CustomOrder order;
        private BehaviorReporter behaviorReporter;

        private void Awake()
        {
            enabled = NetworkManager.Singleton.IsServer;
            behaviorReporter = GetComponent<BehaviorReporter>();
        }

        private void Update()
        {
            FinishOrderIfDone();
        }

        private void FinishOrderIfDone()
        {
            if (order == null || !order.IsDone(gameObject)) return;

            FinishOrder();
        }

        public void FinishOrder()
        {
            if (order == null)
            {
                throw new InvalidOperationException($"Entity {name} has no custom order to finish");
            }

            string outcome = order.GetPromptOutcome(gameObject);
            Log.Info($"Entity {name} finished {order.Kind} custom order: {outcome}");
            order = null;
            behaviorReporter.Report(new BehaviorReport { Prompt = outcome, IsUrgent = true });
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
                State = state == null ? default : SaveToken.FromObject(state)
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData saveData = content.ToObject<SaveData>();
            order = null;

            if (saveData.Kind == null) return;

            CustomOrder loaded = CustomOrder.CreateOfKind(saveData.Kind);
            if (loaded == null)
            {
                Log.Warn($"Entity {name} lost its order: kind {saveData.Kind} is unknown");
                return;
            }

            if (!saveData.State.IsEmpty) loaded.LoadObject(saveData.State);
            order = loaded;
        }

        public CustomOrder CurrentOrder => order;

        public void ForcePutOrder(CustomOrder newOrder)
        {
            if (newOrder == null)
            {
                throw new ArgumentNullException(nameof(newOrder));
            }
            FinishOrderIfDone();
            order = newOrder;
        }

        public bool TryPutOrder(CustomOrder newOrder)
        {
            if (newOrder == null)
            {
                throw new ArgumentNullException(nameof(newOrder));
            }
            FinishOrderIfDone();
            if (order == null)
            {
                order = newOrder;
                return true;
            }
            return false;
        }

        public void ClearOrder()
        {
            FinishOrderIfDone();
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
            return "Second-level behaviour tree action: " + order.GetPromptDescription(gameObject);
        }

        public DigestionPriority Priority => DigestionPriority.High;
    }
}
