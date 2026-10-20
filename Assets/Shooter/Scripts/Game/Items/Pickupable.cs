using System;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(NetworkObject))]
    public class Pickupable : ShooterBehaviour, IUsable, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        private NetworkObject networkObject;
        [SerializeField] private StackableItemAsset item;
        [SerializeField] private SoundAsset sound;

        public Action<Pickupable> PickedUp;

        protected override void ShooterAwake()
        {
            networkObject = GetComponent<NetworkObject>();
            if (item == null)
            {
                Log.Error($"Entity {name} is missing its item");
            }
            if (sound == null)
            {
                Log.Error($"Entity {name} is missing its sound");
            }
        }

        public UsageType Usage => UsageType.PickUp;
        public void Use(NetworkObject user, out string promptResult)
        {
            Log.Info($"Entity {name} has been picked up by {user.name}");
            if (user.TryGetComponent(out Speaker speaker))
            {
                speaker.PlayForEveryone(sound);
            }
            else
            {
                Log.Warn($"Failed to find speaker of user {user.name}");
            }
            if (user.TryGetComponent(out Inventory inventory))
            {
                inventory.AddStackable(item, 1);
            }
            else
            {
                Log.Warn($"Failed to find inventory of user {user.name}");
            }
            PickedUp?.Invoke(this);
            networkObject.Despawn(!networkObject.InScenePlaced);
            gameObject.SetActive(false);
            promptResult = $"You picked up {item.Id}";
        }

        public DigestionPriority Priority => DigestionPriority.High;
        public string Digest(DigestionDetail detail)
        {
            return $"Can be picked up ({item.Id})";
        }
    }
}
