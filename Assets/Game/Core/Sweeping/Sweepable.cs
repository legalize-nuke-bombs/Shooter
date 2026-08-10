using Unity.Netcode;
using UnityEngine;
using Shooter.Game.World;
using Environment = Shooter.Game.World.Environment;

namespace Shooter.Game.Core
{
    public class Sweepable : NetworkBehaviour
    {
        private ISweepingRule[] rules;

        private void Awake()
        {
            rules = GetComponentsInChildren<ISweepingRule>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer) Environment.Current.Sweeper.Adopt(this);
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                Environment world = Environment.Current;
                if (world != null) world.Sweeper.Drop(this);
            }

            gameObject.SetActive(false);
        }

        public bool CanBeSwept()
        {
            foreach (ISweepingRule rule in rules)
                if (rule.Permits)
                    return true;

            return false;
        }

        public void Sweep()
        {
            NetworkObject.Despawn(false);
        }
    }
}
