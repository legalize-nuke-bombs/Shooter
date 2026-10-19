using System.Linq;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(RespawnPosition))]
    [RequireComponent(typeof(Speaker))]
    public class Sleeper : NetworkBehaviour, IMortal, IDigestible, IRestraint, ISaveableComponent
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        [SerializeField] private SoundAsset bedding;

        [SerializeField] private SoundAsset rising;
        private readonly NetworkVariable<Vector3> bedside = new();

        private readonly NetworkVariable<bool> sleeping = new();

        public string ComponentKey => "Sleeper";
        private struct SaveData
        {
            public Vector3 Bedside { get; set; }
            public bool Sleeping { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData()
            {
                Bedside = bedside.Value,
                Sleeping = sleeping.Value,
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.ToObject<SaveData>();
            bedside.Value = sd.Bedside;
            sleeping.Value = sd.Sleeping;
        }

        private SleepCycle sleepCycle;
        private RespawnPosition respawnPosition;
        private Speaker speaker;

        private void Awake()
        {
            sleepCycle = Registers.GetAll<SleepCycle>(RegisterInactivePolicy.Include).Single();
            respawnPosition = GetComponent<RespawnPosition>();
            speaker = GetComponent<Speaker>();
        }

        public bool Sleeping => sleeping.Value;

        public Vector3 Bedside => bedside.Value;

        public string Digest(DigestionDetail detail)
        {
            return Sleeping ? "Asleep" : null;
        }

        public DigestionPriority Priority => DigestionPriority.Low;

        public void Died()
        {
            Rouse(false);
        }

        public bool CanPerform(ActionType type, float dt)
        {
            return !Sleeping;
        }

        public void RegisterAction(ActionType type, float dt)
        {
        }

        public void FallAsleep(Bed bed)
        {
            if (!IsServer || Sleeping) return;

            bedside.Value = bed == null ? transform.position : bed.transform.position;
            sleeping.Value = true;
            respawnPosition.SetPosition(transform.position);
            speaker.PlayForEveryone(bedding);
            Log.Info($"Entity {name} fell asleep at {transform.position} in a bed at {bedside.Value}");
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void WakeRpc()
        {
            bool worldAsleep = sleepCycle.WorldAsleep;

            if (!SleepRule.CanWake(worldAsleep))
            {
                Log.Info($"Entity {name} can not wake up on its own, the whole world is asleep");
                return;
            }

            WakeUp();
        }

        public void WakeUp()
        {
            Rouse(true);
        }

        private void Rouse(bool heard)
        {
            if (!IsServer || !Sleeping) return;

            sleeping.Value = false;
            if (heard) speaker.PlayForEveryone(rising);
            Log.Info($"Entity {name} woke up at {transform.position}");
        }
    }
}
