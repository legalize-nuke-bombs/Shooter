using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Sweeping
{
    public class AutoSweepable : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private ISweepable[] sweepables;

        public void Awake()
        {
            sweepables = GetComponentsInChildren<ISweepable>();
        }

        [SerializeField] private float checkInterval = 1f;
        private float timeSinceLastCheck = 0f;
        public void Update()
        {
            if (!IsSpawned || !IsServer) return;

            timeSinceLastCheck += Time.deltaTime;
            if (timeSinceLastCheck >= checkInterval)
            {
                timeSinceLastCheck = 0f;
                foreach (ISweepable sweepable in sweepables)
                {
                    if (sweepable.CanBeSwept())
                    {
                        Log.Info("Entity {} will be despawned", name);
                        NetworkObject.Despawn(false);
                        return;
                    }
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            gameObject.SetActive(false);
        }
    }
}
