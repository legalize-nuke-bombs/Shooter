using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(InventoryUser))]
    [RequireComponent(typeof(Speaker))]
    public class SoundOnItemUse : ShooterBehaviour
    {
        private InventoryUser inventoryUser;
        private Speaker speaker;

        protected override void ShooterAwake()
        {
            inventoryUser = GetComponent<InventoryUser>();
            speaker = GetComponent<Speaker>();
        }

        private void OnEnable()
        {
            inventoryUser.Used += Play;
        }

        private void OnDisable()
        {
            inventoryUser.Used -= Play;
        }

        private void Play(StackableItemAsset asset)
        {
            speaker.Play(asset.UseSound);
        }
    }
}
