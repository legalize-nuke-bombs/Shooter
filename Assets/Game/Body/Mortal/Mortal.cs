using Shooter.Game.Core;
using Shooter.Game.Loot;
using Shooter.Game.World;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body
{
    [RequireComponent(typeof(Health))]
    // Movement is not required
    [RequireComponent(typeof(EarSpeaker))]
    // Sleeper is not required
    public class Mortal : NetworkBehaviour, IMortal
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private GameObject corpsePrefab;

        [SerializeField] private EarSoundSpec deathSound;
        private EarSpeaker earSpeaker;

        private Health health;
        private Movement movement;
        private Sleeper sleeper;

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

            if (!NetworkObject.IsPlayerObject) NetworkObject.Despawn(!NetworkObject.InScenePlaced);
        }

        public override void OnNetworkDespawn()
        {
            gameObject.SetActive(false);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void RiseRpc()
        {
            if (health == null || health.Alive) return;

            Vector3 at = SpawnPoint();
            movement?.Teleport(at);
            health.Resurrect();
            Log.Info($"Entity {this.NameOf()} rose at {at}");
        }

        private Vector3 SpawnPoint()
        {
            if (sleeper != null) return sleeper.SpawnPoint;

            return MainSpawnPoint.Current == null ? transform.position : MainSpawnPoint.Current.transform.position;
        }

        private void LeaveCorpse()
        {
            GameObject prefab = CorpsePrefab();
            if (prefab == null)
            {
                Log.Warn($"Entity {this.NameOf()} died, but neither it nor the world has a corpse prefab");
                return;
            }

            GameObject body = Instantiate(prefab, transform.position, transform.rotation);
            NetworkObject spawned = body.GetComponent<NetworkObject>();
            if (spawned == null)
            {
                Log.Error($"Corpse prefab of entity {this.NameOf()} has no network object");
                Destroy(body);
                return;
            }

            spawned.Spawn();

            Corpse corpse = body.GetComponent<Corpse>();
            if (corpse != null)
            {
                Skin skin = GetComponent<Skin>();
                if (skin != null && skin.Spec != null) corpse.Dress(skin.Spec);

                TypedNameable named = GetComponent<TypedNameable>();
                if (named != null && named.Spec != null) corpse.Rename(named.Spec);
            }

            spawned.GetComponent<Lootable>()?.Fill(this.Find<Inventory>());
            Log.Info($"Entity {this.NameOf()} left a corpse at {transform.position}");
        }

        private GameObject CorpsePrefab()
        {
            return corpsePrefab;
        }
    }
}
