using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Player))]
    [RequireComponent(typeof(InventoryHolder))]
    [RequireComponent(typeof(Gunner))]
    public class LocalRecoil : ShooterNetworkComponent, IShooterUpdateable
    {
        [SerializeField] private float recovery = 14f;

        private readonly ShotCadence cadence = new();
        private Player player;
        private InventoryHolder inventoryHolder;
        private Gunner gunner;
        private bool triggerHeld;

        public Vector2 PunchAngles { get; private set; }

        protected override void ShooterAwake()
        {
            player = GetComponent<Player>();
            inventoryHolder = GetComponent<InventoryHolder>();
            gunner = GetComponent<Gunner>();
        }

        public void ShooterUpdate()
        {
            PunchAngles *= Mathf.Exp(-recovery * Time.deltaTime);

            if (triggerHeld) PredictShot(true);
        }

        public override void OnNetworkSpawn()
        {
            player.ControlChanged += HandleControlChanged;
            if (IsServer) gunner.Shot += HandleShot;
            HandleControlChanged();
        }

        public override void OnNetworkDespawn()
        {
            player.ControlChanged -= HandleControlChanged;
            if (IsServer) gunner.Shot -= HandleShot;
        }

        private void HandleControlChanged()
        {
            enabled = player.IsControlledLocally;
            triggerHeld = false;
            PunchAngles = Vector2.zero;
        }

        public void PressTrigger()
        {
            if (IsServer) return;

            triggerHeld = true;
            PredictShot(false);
        }

        public void ReleaseTrigger()
        {
            triggerHeld = false;
        }

        private void HandleShot(FirearmAsset firearmAsset, ShotHit hit)
        {
            if (!player.IsControlledLocally) return;

            AddPunch(firearmAsset);
        }

        private void PredictShot(bool sustained)
        {
            Firearm firearm = inventoryHolder.HeldItem as Firearm;
            if (firearm == null || firearm.Magazine <= 0) return;

            FirearmAsset firearmAsset = Catalogs.GetCatalog<ItemCatalog>().GetFirearmAssetById(firearm.AssetId);
            if (firearmAsset == null) return;
            if (!cadence.CanFire(firearmAsset, sustained, Time.time)) return;

            AddPunch(firearmAsset);
        }

        private void AddPunch(FirearmAsset firearmAsset)
        {
            int shotIndex = cadence.Fire(firearmAsset, Time.time);
            Vector2 stepToNextShot = firearmAsset.Spray.GetOffsetForShot(shotIndex + 1) - firearmAsset.Spray.GetOffsetForShot(shotIndex);
            PunchAngles += stepToNextShot * firearmAsset.RecoilPunch;
        }
    }
}
