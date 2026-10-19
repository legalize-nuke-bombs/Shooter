using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(InventoryHolder))]
    public class OwnRecoil : NetworkBehaviour
    {
        [SerializeField] private float recovery = 14f;

        private InventoryHolder inventoryHolder;
        private bool triggerHeld;
        private float lastShotTime;
        private int sprayShotIndex;

        public Vector2 PunchAngles { get; private set; }

        private void Awake()
        {
            inventoryHolder = GetComponent<InventoryHolder>();
        }

        private void Update()
        {
            PunchAngles *= Mathf.Exp(-recovery * Time.deltaTime);

            if (triggerHeld) AddPunchIfShotFires(true);
        }

        public override void OnNetworkSpawn()
        {
            enabled = IsOwner;
        }

        public override void OnGainedOwnership()
        {
            if (IsOwner) enabled = true;
        }

        public override void OnLostOwnership()
        {
            enabled = false;
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
            if (sustained && firearmAsset.FireMode != FireMode.Auto) return;

            if (Time.time - lastShotTime < firearmAsset.FireInterval) return;
            if (Time.time - lastShotTime >= firearmAsset.SprayRecovery) sprayShotIndex = 0;

            Vector2 previousOffset = sprayShotIndex == 0 ? Vector2.zero : firearmAsset.Spray.GetOffsetForShot(sprayShotIndex - 1);
            PunchAngles += (firearmAsset.Spray.GetOffsetForShot(sprayShotIndex) - previousOffset) * firearmAsset.RecoilPunch;

            sprayShotIndex++;
            lastShotTime = Time.time;
        }
    }
}
