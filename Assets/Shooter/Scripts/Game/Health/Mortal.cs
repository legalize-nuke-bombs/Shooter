using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Movement))]
    [RequireComponent(typeof(EarSpeaker))]
    [RequireComponent(typeof(RespawnPosition))]
    public class Mortal : ShooterNetworkBehaviour, IMortal
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private GameObject corpsePrefab;
        [SerializeField] private EarSoundAsset deathSound;

        private EarSpeaker earSpeaker;
        private Health health;
        private Movement movement;
        private Player player;
        private RespawnPosition respawnPosition;

        protected override void ShooterAwake()
        {
            health = GetComponent<Health>();
            movement = GetComponent<Movement>();
            respawnPosition = GetComponent<RespawnPosition>();
            earSpeaker = GetComponent<EarSpeaker>();
            player = GetComponent<Player>();
        }

        public void HandleDeath()
        {
            if (!IsServer) return;

            earSpeaker.PlayForOwner(deathSound);
            LeaveCorpse();

            if (player == null) NetworkObject.Despawn(!NetworkObject.InScenePlaced);
        }

        public override void OnNetworkDespawn()
        {
            gameObject.SetActive(false);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void RiseRpc()
        {
            if (health == null || health.IsAlive) return;

            Vector3 position = respawnPosition.GetPosition();
            movement.Teleport(position);
            health.ResurrectWithFullHp();
            Log.Info($"Entity {name} rose at {position}");
        }

        private void LeaveCorpse()
        {
            if (corpsePrefab == null)
            {
                Log.Error($"Entity {name} died without a corpse prefab");
                return;
            }

            GameObject corpseObject = NetworkSpawner.InstantiateAndSpawn(corpsePrefab, transform.position, transform.rotation);
            if (corpseObject == null)
            {
                Log.Error($"Entity {name} failed to spawn corpse");
                return;
            }

            Corpse corpse = corpseObject.GetComponent<Corpse>();
            if (corpse != null)
            {
                Skin skin = GetComponent<Skin>();
                if (skin != null && skin.Asset != null) corpse.AssignSkinAsset(skin.Asset);

                AssetNameable assetNameable = GetComponent<AssetNameable>();
                if (assetNameable != null && assetNameable.Asset != null) corpse.AssignNameAsset(assetNameable.Asset);
            }

            corpseObject.GetComponent<Lootable>()?.TakeAllItemsFrom(GetComponent<Inventory>());
            Log.Info($"Entity {name} left a corpse at {transform.position}");
        }
    }
}
