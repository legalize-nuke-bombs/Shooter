using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InventoryUser))]
    [RequireComponent(typeof(Speaker))]
    public class SoundOnItemUse : ShooterComponent
    {
        private InventoryUser inventoryUser;
        private Speaker speaker;

        protected override void ShooterAwake()
        {
            inventoryUser = GetComponent<InventoryUser>();
            speaker = GetComponent<Speaker>();
        }

        protected override void ShooterOnEnable()
        {
            inventoryUser.Used += Play;
        }

        protected override void ShooterOnDisable()
        {
            inventoryUser.Used -= Play;
        }

        private void Play(StackableItemAsset asset)
        {
            speaker.Play(asset.UseSound);
        }
    }
}
