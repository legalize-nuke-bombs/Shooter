using Unity.Netcode;
using Random = UnityEngine.Random;

namespace Shooter.Infrastructure
{
    public class IncrementalId : ShooterNetworkBehaviour, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        public const long None = -1;

        private readonly NetworkVariable<long> id = new(None);

        public long Value => id.Value;

        public bool IsLoadedFromSave { get; private set; }

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
            IsLoadedFromSave = true;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer || id.Value != None) return;

            if (!Registers.Instance.GetAll<IncrementalIds>(RegisterInactivePolicy.Include).TryGetFirst(out IncrementalIds incrementalIds))
            {
                Log.Error($"Entity {name} failed to find IncrementalIds. Fall back to random id generation");
                id.Value = Random.Range(0, int.MaxValue);
            }
            else
            {
                id.Value = incrementalIds.TakeNextId();
            }
        }

        internal void Renumber(long newId)
        {
            id.Value = newId;
        }

        public static IncrementalId FindByValue(long id, RegisterInactivePolicy inactivePolicy)
        {
            foreach (IncrementalId target in Registers.Instance.GetAll<IncrementalId>(inactivePolicy))
            {
                if (target.Value == id)
                {
                    return target;
                }
            }
            return null;
        }
    }
}
