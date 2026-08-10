using Shooter.Game.Loot;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;
using Environment = Shooter.Game.World.Environment;

namespace Shooter.Game.Body
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Movement))]
    [RequireComponent(typeof(EarSpeaker))]
    // Sleeper is not required
    public class Mortal : NetworkBehaviour, IMortal
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private GameObject corpsePrefab;

        [SerializeField] private EarSoundSpec deathSound;

        private Health health;
        private Movement movement;
        private Sleeper sleeper;
        private EarSpeaker earSpeaker;

        private void Awake()
        {
            health = GetComponent<Health>();
            movement = GetComponent<Movement>();
            sleeper = GetComponent<Sleeper>();
            earSpeaker = GetComponent<EarSpeaker>();
        }

        public void Died()
        {
            if (!IsServer) return;

            earSpeaker.Play(deathSound);
            LeaveCorpse();

            if (!NetworkObject.IsPlayerObject)
                NetworkObject.Despawn();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void RiseRpc()
        {
            if (health == null || health.Alive) return;

            Vector3 at = SpawnPoint();
            movement?.Teleport(at);
            health.Resurrect();
            Log.Info($"Entity {name} rose at {at}");
        }

        private Vector3 SpawnPoint()
        {
            if (sleeper != null) return sleeper.SpawnPoint;

            return Environment.Current == null ? transform.position : Environment.Current.Spawn.position;
        }

        private void LeaveCorpse()
        {
            GameObject prefab = CorpsePrefab();
            if (prefab == null)
            {
                Log.Warn($"Entity {name} died, but neither it nor the world has a corpse prefab");
                return;
            }

            GameObject body = Instantiate(prefab, transform.position, transform.rotation);
            var spawned = body.GetComponent<NetworkObject>();
            if (spawned == null)
            {
                Log.Error($"Corpse prefab of entity {name} has no network object");
                Destroy(body);
                return;
            }

            var corpse = body.GetComponent<Corpse>();
            if (corpse != null)
            {
                var skin = GetComponent<Skin>();
                if (skin != null && skin.Spec != null) corpse.Dress(skin.Spec);

                var named = GetComponent<TypedNameable>();
                if (named != null && named.Spec != null) corpse.Rename(named.Spec);
            }

            spawned.Spawn();
            spawned.GetComponent<Lootable>()?.Fill(GetComponent<Inventory>());
            Log.Info($"Entity {name} left a corpse at {transform.position}");
        }

        private GameObject CorpsePrefab()
        {
            if (corpsePrefab != null) return corpsePrefab;

            return Environment.Current == null ? null : Environment.Current.Corpse;
        }
    }
}
