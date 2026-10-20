using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(RespawnPosition))]
    [RequireComponent(typeof(Speaker))]
    [RequireComponent(typeof(Health))]
    public class Sleeper : ShooterNetworkBehaviour, IDigestible, IRestraint, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private SoundAsset bedding;
        [SerializeField] private SoundAsset rising;

        private readonly NetworkVariable<Vector3> bedPosition = new();
        private readonly NetworkVariable<bool> sleeping = new();

        private Health health;
        private SleepCycle sleepCycle;
        private RespawnPosition respawnPosition;
        private Speaker speaker;

        public string ComponentKey => "Sleeper";

        private struct SaveData
        {
            public Vector3 Bedside { get; set; }
            public bool Sleeping { get; set; }
        }

        public object SaveObject()
        {
            return new SaveData
            {
                Bedside = bedPosition.Value,
                Sleeping = sleeping.Value
            };
        }

        public void LoadObject(SaveToken content)
        {
            SaveData saveData = content.ToObject<SaveData>();
            bedPosition.Value = saveData.Bedside;
            sleeping.Value = saveData.Sleeping;
        }

        protected override void ShooterAwake()
        {
            health = GetComponent<Health>();
            sleepCycle = Registers.Get<SleepCycle>();
            respawnPosition = GetComponent<RespawnPosition>();
            speaker = GetComponent<Speaker>();
        }

        private void OnEnable()
        {
            health.Died += StopSleepingOnDeath;
        }

        private void OnDisable()
        {
            health.Died -= StopSleepingOnDeath;
        }

        public bool IsSleeping => sleeping.Value;

        public Vector3 BedPosition => bedPosition.Value;

        public string Digest(DigestionDetail detail)
        {
            return IsSleeping ? "Asleep" : null;
        }

        public DigestionPriority Priority => DigestionPriority.Low;

        private void StopSleepingOnDeath()
        {
            StopSleeping(false);
        }

        public bool CanPerform(ActionType type, float dt)
        {
            return !IsSleeping;
        }

        public void RegisterAction(ActionType type, float dt)
        {
        }

        public void FallAsleep(Bed bed)
        {
            if (!IsServer || IsSleeping) return;

            bedPosition.Value = bed == null ? transform.position : bed.transform.position;
            sleeping.Value = true;
            respawnPosition.SetPosition(transform.position);
            speaker.PlayForEveryone(bedding);
            Log.Info($"Entity {name} fell asleep at {transform.position} in a bed at {bedPosition.Value}");
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void WakeRpc()
        {
            bool worldAsleep = sleepCycle.IsWorldAsleep;

            if (!SleepRule.CanWake(worldAsleep))
            {
                Log.Info($"Entity {name} can not wake up on its own, the whole world is asleep");
                return;
            }

            WakeUp();
        }

        public void WakeUp()
        {
            StopSleeping(true);
        }

        private void StopSleeping(bool playRisingSound)
        {
            if (!IsServer || !IsSleeping) return;

            sleeping.Value = false;
            if (playRisingSound) speaker.PlayForEveryone(rising);
            Log.Info($"Entity {name} woke up at {transform.position}");
        }
    }
}
