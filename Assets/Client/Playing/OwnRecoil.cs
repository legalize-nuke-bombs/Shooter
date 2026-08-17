using Shooter.Game.Combat;
using Shooter.Game.Loot;
using Unity.Netcode;
using UnityEngine;
using Shooter.Game.Core;

namespace Shooter.Client.Playing
{
    [RequireComponent(typeof(Inventory))]
    public class OwnRecoil : NetworkBehaviour
    {
        [SerializeField] private float recovery = 14f;

        private Inventory inventory;

        private int shot;
        private float lastShotAt;
        private bool held;

        public Vector2 Punch { get; private set; }

        private void Awake()
        {
            inventory = GetComponent<Inventory>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) enabled = false;
        }

        private void Update()
        {
            Punch *= Mathf.Exp(-recovery * Time.deltaTime);

            if (held) Kick(true);
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
