using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Sweeping
{
    [RequireComponent(typeof(NetworkObject))]
    public class AutoSweepable : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private NetworkObject networkObject;
        private ISweepable[] sweepables;

        public void Awake()
        {
            networkObject = GetComponent<NetworkObject>();
            sweepables = GetComponentsInChildren<ISweepable>();
        }

        [SerializeField] private float checkInterval = 1f;
        private float timeSinceLastCheck = 0f;
        public void Update()
        {
            timeSinceLastCheck += Time.deltaTime;
            if (timeSinceLastCheck >= checkInterval)
            {
                timeSinceLastCheck = 0f;
                foreach (ISweepable sweepable in sweepables) {
                    if (sweepable.CanBeSwept())
                    {
                        Log.Info("Entity {} will be despawned", name);
                        networkObject.Despawn(false);
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
}
