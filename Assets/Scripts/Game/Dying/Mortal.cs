using Unity.Netcode;
using UnityEngine;
using Shooter.Game.Items;
using Shooter.Game.Sleeping;
using Shooter.Logging;

namespace Shooter.Game.Dying
{
    public class Mortal : NetworkBehaviour, IMortal
    {
        [SerializeField] private GameObject corpsePrefab;

        private Health.Health health;
        private Movement.Movement movement;
        private Sleeper sleeper;

        private void Awake()
        {
            health = GetComponent<Health.Health>();
            movement = GetComponent<Movement.Movement>();
            sleeper = GetComponent<Sleeper>();
        }

        public void Died()
        {
            if (!IsServer) return;

            LeaveCorpse();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void RiseRpc()
        {
            if (health == null || health.Alive) return;

            Vector3 at = sleeper == null ? Vector3.zero : sleeper.SpawnPoint;
            movement?.Teleport(at);
            health.Resurrect();
            Log.Info("Entity {} rose at {}", name, at);
        }

        private void LeaveCorpse()
        {
            if (corpsePrefab == null)
            {
                Log.Warn("Entity {} died without a corpse prefab", name);
                return;
            }

            GameObject body = Instantiate(corpsePrefab, transform.position, transform.rotation);
            var spawned = body.GetComponent<NetworkObject>();
            if (spawned == null)
            {
                Log.Error("Corpse prefab of entity {} has no network object", name);
                Destroy(body);
                return;
            }

            spawned.Spawn();
            spawned.GetComponent<Lootable>()?.Fill(GetComponent<Inventory>());
            Log.Info("Entity {} left a corpse at {}", name, transform.position);
        }
    }
}
