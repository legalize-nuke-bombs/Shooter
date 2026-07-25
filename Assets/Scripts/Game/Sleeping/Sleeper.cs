using Unity.Netcode;
using UnityEngine;
using Shooter.Logging;

namespace Shooter.Game.Sleeping
{
    public class Sleeper : NetworkBehaviour, IMortal, IDigestible, IRestraint
    {
        private readonly NetworkVariable<bool> sleeping = new NetworkVariable<bool>();

        public bool Sleeping => sleeping.Value;

        public bool Restrains => Sleeping;

        public Vector3 SpawnPoint { get; private set; }

        public void FallAsleep()
        {
            if (!IsServer || Sleeping) return;

            sleeping.Value = true;
            SpawnPoint = transform.position;
            Log.Info("Entity {} fell asleep at {}", name, SpawnPoint);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void WakeRpc()
        {
            WakeUp();
        }

        public void WakeUp()
        {
            if (!IsServer || !Sleeping) return;

            sleeping.Value = false;
            Log.Info("Entity {} woke up at {}", name, transform.position);
        }

        public void Died()
        {
            WakeUp();
        }

        public string Digest()
        {
            return Sleeping ? "Спит" : null;
        }
    }
}
