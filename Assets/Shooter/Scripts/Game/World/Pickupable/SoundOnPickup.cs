using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Pickupable))]
    public class SoundOnPickup : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private SoundAsset sound;

        private Pickupable pickupable;

        private void Awake()
        {
            pickupable = GetComponent<Pickupable>();
        }

        private void OnEnable()
        {
            pickupable.PickedUp += PlayAtUser;
        }

        private void OnDisable()
        {
            pickupable.PickedUp -= PlayAtUser;
        }

        private void PlayAtUser(Pickupable picked, NetworkObject user)
        {
            if (!user.TryGetComponent(out Speaker speaker))
            {
                Log.Warn($"Failed to find speaker of user {user.name}");
                return;
            }

            speaker.Play(sound);
        }
    }
}
