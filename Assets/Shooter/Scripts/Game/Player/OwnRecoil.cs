using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(InventoryHolder))]
    public class OwnRecoil : NetworkBehaviour
    {
        [SerializeField] private float recovery = 14f;
        private bool held;

        private InventoryHolder holder;
        private float lastShotAt;

        private int shot;

        public Vector2 Punch { get; private set; }

        private void Awake()
        {
            holder = GetComponent<InventoryHolder>();
        }

        private void Update()
        {
            Punch *= Mathf.Exp(-recovery * Time.deltaTime);

            if (held) Kick(true);
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
            held = false;
            Punch = Vector2.zero;
        }

        public void Press()
        {
            held = true;
            Kick(false);
        }

        public void Release()
        {
            held = false;
        }

        private void Kick(bool sustained)
        {
            var firearm = holder.HeldItem as Firearm;
            if (firearm == null || firearm.RoundsInMagazine <= 0) return;

            FirearmAsset asset = Catalogs.GetCatalog<ItemCatalog>().GetFirearmAssetById(firearm.AssetId);
            if (asset == null) return;
            if (sustained && asset.FireMode != FireMode.Auto) return;

            if (Time.time - lastShotAt < asset.FireInterval) return;
            if (Time.time - lastShotAt >= asset.SprayRecovery) shot = 0;

            Vector2 previous = shot == 0 ? Vector2.zero : asset.Spray.GetOffsetForShot(shot - 1);
            Punch += (asset.Spray.GetOffsetForShot(shot) - previous) * asset.RecoilPunch;

            shot++;
            lastShotAt = Time.time;
        }
    }
}
