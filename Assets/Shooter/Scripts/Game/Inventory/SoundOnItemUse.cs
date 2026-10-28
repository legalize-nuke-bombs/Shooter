using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InventoryUser))]
    [RequireComponent(typeof(Speaker))]
    public class SoundOnItemUse : MonoBehaviour
    {
        private InventoryUser inventoryUser;
        private Speaker speaker;

        private void Awake()
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
