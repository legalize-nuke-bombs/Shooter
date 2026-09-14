using System;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Loot
{
    [RequireComponent(typeof(NetworkObject))]
    public class Pickupable : MonoBehaviour, IUsable, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        private NetworkObject networkObject;
        [SerializeField] private StackableItemSpec item;
        [SerializeField] private SoundSpec sound;

        public Action<Pickupable> OnPickup;

        private void Awake()
        {
            networkObject = GetComponent<NetworkObject>();
            if (item == null)
            {
                Log.Error($"Entity {name} is missing its item");
            }
            if (sound == null)
            {
                Log.Warn($"Entity {name} is missing its sound");
            }
        }

        public UsageType Usage => UsageType.PickUp;
        public void Use(NetworkObject user)
        {
            Log.Info($"Entity {name} has been picked up by {user.name}");
            if (user.TryGetComponent(out Speaker speaker))
            {
                speaker.Play(sound);
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
            OnPickup?.Invoke(this);
            networkObject.Despawn(!networkObject.InScenePlaced);
        }

        public DigestionPriority Priority => DigestionPriority.High;
        public string Digest(DigestionDetail detail)
        {
            return $"Can be picked up ({item.Id})";
        }
    }
}
