using Shooter.Game.Core;
using Shooter.Game.World;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body
{
    [RequireComponent(typeof(Speaker))]
    public class Sleeper : NetworkBehaviour, IMortal, IDigestible, IRestraint
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private SoundSpec bedding;

        [SerializeField] private SoundSpec rising;
        private readonly NetworkVariable<Vector3> bedside = new();

        private readonly NetworkVariable<bool> sleeping = new();

        private Vector3? bedded;

        private Speaker speaker;

        public bool Sleeping => sleeping.Value;

        public Vector3 Bedside => bedside.Value;

        public Vector3 SpawnPoint
        {
            get
            {
                if (bedded.HasValue) return bedded.Value;

                return MainSpawnPoint.Current == null ? transform.position : MainSpawnPoint.Current.transform.position;
            }
        }

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
            bedded = transform.position;
            Sound(bedding);
            Log.Info($"Entity {this.NameOf()} fell asleep at {SpawnPoint} in a bed at {bedside.Value}");
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void WakeRpc()
        {
            bool worldAsleep = SleepCycle.Current != null && SleepCycle.Current.WorldAsleep;

            if (!SleepRule.CanWake(worldAsleep))
            {
                Log.Info($"Entity {this.NameOf()} can not wake up on its own, the whole world is asleep");
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
            if (heard) Sound(rising);
            Log.Info($"Entity {this.NameOf()} woke up at {transform.position}");
        }

        private void Sound(SoundSpec sound)
        {
            if (speaker == null) speaker = GetComponent<Speaker>();

            if (speaker != null) speaker.Play(sound);
        }
    }
}
