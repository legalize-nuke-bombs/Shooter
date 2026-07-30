using Shooter.Game.Body.Sleeping;
using Shooter.Game.Loot;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body
{
    public class Mortal : NetworkBehaviour, IMortal
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private GameObject corpsePrefab;

        private Health health;
        private Movement movement;
        private Sleeper sleeper;

        private void Awake()
        {
            health = GetComponent<Health>();
            movement = GetComponent<Movement>();
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
            GameObject prefab = CorpsePrefab();
            if (prefab == null)
            {
                Log.Warn("Entity {} died, but neither it nor the world has a corpse prefab", name);
                return;
            }

            GameObject body = Instantiate(prefab, transform.position, transform.rotation);
            var spawned = body.GetComponent<NetworkObject>();
            if (spawned == null)
            {
                Log.Error("Corpse prefab of entity {} has no network object", name);
                Destroy(body);
                return;
            }

            var skin = GetComponent<Skin>();
            if (skin != null && skin.Spec != null)
                body.GetComponent<Corpse>()?.Dress(skin.Spec);

            spawned.Spawn();
            spawned.GetComponent<Lootable>()?.Fill(GetComponent<Inventory>());
            Log.Info("Entity {} left a corpse at {}", name, transform.position);
        }

        private GameObject CorpsePrefab()
        {
            if (corpsePrefab != null) return corpsePrefab;

            return Environment.Current == null ? null : Environment.Current.Corpse;
        }
    }
}
