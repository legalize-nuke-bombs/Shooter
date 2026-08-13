using Shooter.Game.Loot;
using Unity.Netcode;
using UnityEngine;
using Environment = Shooter.Game.World.Environment;

namespace Shooter.Client.Playing
{
    [RequireComponent(typeof(Inventory))]
    public class OwnRecoil : NetworkBehaviour
    {
        [SerializeField] private float recovery = 14f;

        private Inventory inventory;

        private int shot;
        private float lastShotAt;

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
        }

        public void Kick()
        {
            var firearm = inventory.Equipped() as Firearm;
            if (firearm == null || firearm.Magazine <= 0) return;

            FirearmSpec spec = Environment.Current.Items.Firearm(firearm.SpecId);
            if (spec == null) return;

            if (Time.time - lastShotAt < spec.FireInterval) return;
            if (Time.time - lastShotAt >= spec.SprayRecovery) shot = 0;

            Vector2 previous = shot == 0 ? Vector2.zero : spec.Spray.At(shot - 1);
            Punch += (spec.Spray.At(shot) - previous) * spec.RecoilPunch;

            shot++;
            lastShotAt = Time.time;
        }
    }
}
