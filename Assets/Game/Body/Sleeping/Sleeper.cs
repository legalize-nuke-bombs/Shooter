using Shooter.Game.Body.Sounding;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body.Sleeping
{
    [RequireComponent(typeof(Speaker))]
    public class Sleeper : NetworkBehaviour, IMortal, IDigestible, IRestraint
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private SoundSpec bedding;

        [SerializeField] private SoundSpec rising;

        private readonly NetworkVariable<bool> sleeping = new NetworkVariable<bool>();
        private readonly NetworkVariable<Vector3> bedside = new NetworkVariable<Vector3>();

        private Speaker speaker;

        public bool Sleeping => sleeping.Value;

        public bool CanPerform(ActionType type, float dt)
        {
            return !Sleeping;
        }
        public void RegisterAction(ActionType type, float dt) { }

        public Vector3 Bedside => bedside.Value;

        private Vector3? bedded;

        public Vector3 SpawnPoint
        {
            get
            {
                if (bedded.HasValue) return bedded.Value;

                return Environment.Current == null ? transform.position : Environment.Current.Spawn.position;
            }
        }

        public void FallAsleep(Bed bed)
        {
            if (!IsServer || Sleeping) return;

            bedside.Value = bed == null ? transform.position : bed.transform.position;
            sleeping.Value = true;
            bedded = transform.position;
            Sound(bedding);
            Log.Info($"Entity {name} fell asleep at {SpawnPoint} in a bed at {bedside.Value}");
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void WakeRpc()
        {
            bool worldAsleep = Environment.Current != null && Environment.Current.SleepCycle.WorldAsleep;

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

        public void Died()
        {
            Rouse(false);
        }

        private void Rouse(bool heard)
        {
            if (!IsServer || !Sleeping) return;

            sleeping.Value = false;
            if (heard) Sound(rising);
            Log.Info($"Entity {name} woke up at {transform.position}");
        }

        private void Sound(SoundSpec sound)
        {
            if (speaker == null) speaker = GetComponent<Speaker>();

            if (speaker != null) speaker.Play(sound);
        }

        public string Digest(DigestionDetail detail)
        {
            return Sleeping ? "Asleep" : null;
        }

        public DigestionPriority Priority => DigestionPriority.Low;
    }
}
