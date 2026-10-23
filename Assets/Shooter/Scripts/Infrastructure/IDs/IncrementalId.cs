using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Infrastructure
{
    public class IncrementalId : ShooterNetworkBehaviour, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        public const long None = -1;

        private readonly NetworkVariable<long> id = new(None);

        public long Value => id.Value;

        private struct SaveData
        {
            public long Id { get; set; }
        }

        public object SaveObject()
        {
            return new SaveData
            {
                Id = Value
            };
        }

        public void LoadObject(SaveToken content)
        {
            id.Value = content.ToObject<SaveData>().Id;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer || id.Value != None) return;

            IncrementalIds incrementalIds = Registers.GetAll<IncrementalIds>(RegisterInactivePolicy.Include).FirstOrDefault();
            if (incrementalIds == null)
            {
                Log.Error($"Entity {name} failed to find IncrementalIds. Fall back to random id generation");
                id.Value = Random.Range(0, int.MaxValue);
            }
            else
            {
                id.Value = incrementalIds.TakeNextId();
            }
        }
    }
}
