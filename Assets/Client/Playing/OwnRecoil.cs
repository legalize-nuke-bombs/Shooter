using Shooter.Game.Combat;
using Shooter.Game.Core;
using Shooter.Game.Loot;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Client.Playing
{
    public class OwnRecoil : NetworkBehaviour
    {
        [SerializeField] private float recovery = 14f;
        private bool held;

        private Inventory inventory;
        private float lastShotAt;

        private int shot;

        public Vector2 Punch { get; private set; }

        private void Awake()
        {
            inventory = this.Find<Inventory>();
        }

        private void Update()
        {
            Punch *= Mathf.Exp(-recovery * Time.deltaTime);

            if (held) Kick(true);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) enabled = false;
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
            var firearm = inventory.Equipped() as Firearm;
            if (firearm == null || firearm.Magazine <= 0) return;

            FirearmSpec spec = Catalogs.Of<ItemCatalog>().Firearm(firearm.SpecId);
            if (spec == null) return;
            if (sustained && spec.FireMode != FireMode.Auto) return;

            if (Time.time - lastShotAt < spec.FireInterval) return;
            if (Time.time - lastShotAt >= spec.SprayRecovery) shot = 0;

            Vector2 previous = shot == 0 ? Vector2.zero : spec.Spray.At(shot - 1);
            Punch += (spec.Spray.At(shot) - previous) * spec.RecoilPunch;

            shot++;
            lastShotAt = Time.time;
        }
    }
}
