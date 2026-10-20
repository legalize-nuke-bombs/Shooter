using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    // On the host Gunner fires in the same frame: the camera kicks first, or the last round of the magazine would be
    // spent before the kick sees it
    [DefaultExecutionOrder(ExecutionOrder.LocalPrediction)]
    [RequireComponent(typeof(Player))]
    [RequireComponent(typeof(InventoryHolder))]
    public class LocalRecoil : ShooterNetworkBehaviour
    {
        [SerializeField] private float recovery = 14f;

        private readonly ShotCadence cadence = new();
        private Player player;
        private InventoryHolder inventoryHolder;
        private bool triggerHeld;

        public Vector2 PunchAngles { get; private set; }

        protected override void ShooterAwake()
        {
            player = GetComponent<Player>();
            inventoryHolder = GetComponent<InventoryHolder>();
        }

        private void Update()
        {
            PunchAngles *= Mathf.Exp(-recovery * Time.deltaTime);

            if (triggerHeld) AddPunchIfShotFires(true);
        }

        public override void OnNetworkSpawn()
        {
            player.ControlChanged += HandleControlChanged;
            HandleControlChanged();
        }

        public override void OnNetworkDespawn()
        {
            player.ControlChanged -= HandleControlChanged;
        }

        private void HandleControlChanged()
        {
            enabled = player.IsControlledLocally;
            triggerHeld = false;
            PunchAngles = Vector2.zero;
        }

        public void PressTrigger()
        {
            triggerHeld = true;
            AddPunchIfShotFires(false);
        }

        public void ReleaseTrigger()
        {
            triggerHeld = false;
        }

        private void AddPunchIfShotFires(bool sustained)
        {
            Firearm firearm = inventoryHolder.HeldItem as Firearm;
            if (firearm == null || firearm.RoundsInMagazine <= 0) return;

            FirearmAsset firearmAsset = Catalogs.GetCatalog<ItemCatalog>().GetFirearmAssetById(firearm.AssetId);
            if (firearmAsset == null) return;
            if (!cadence.CanFire(firearmAsset, sustained, Time.time)) return;

            int shotIndex = cadence.Fire(firearmAsset, Time.time);
            Vector2 previousOffset = shotIndex == 0 ? Vector2.zero : firearmAsset.Spray.GetOffsetForShot(shotIndex - 1);
            PunchAngles += (firearmAsset.Spray.GetOffsetForShot(shotIndex) - previousOffset) * firearmAsset.RecoilPunch;
        }
    }
}
