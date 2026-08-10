using System.Collections.Generic;
using Shooter.Game.Core;
using Shooter.Game.Notifying;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.World
{
    [RequireComponent(typeof(SphereCollider))]
    public class AreaNotificationPublisher : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private NotificationSpec notificationSpec;

        private static int characterLayer = -1;

        private static int CharacterLayer =>
            characterLayer != -1 ? characterLayer : characterLayer = LayerMask.NameToLayer("Character");

        private HashSet<long> done = new HashSet<long>();

        private void Awake()
        {
            SphereCollider sphere = GetComponent<SphereCollider>();
            if (!sphere.isTrigger) Log.Warn($"Sphere must be trigger!");
        }

        private void OnTriggerEnter(Collider target)
        {
            NetworkManager network = NetworkManager.Singleton;
            if (network == null || !network.IsServer) return;

            if (target.gameObject.layer != CharacterLayer) return;

            MainNotificationRecipient notificationRecipient = target.GetComponentInParent<MainNotificationRecipient>();
            if (notificationRecipient == null) return;

            PersistentId persistentId = target.GetComponentInParent<PersistentId>();
            if (persistentId == null)
            {
                Log.Warn($"Entity {notificationRecipient.name} has notification recipient but does not have persistent id");
                return;
            }

            if (notificationSpec == null)
            {
                Log.Warn($"Entity {name} does not have a notification spec");
                return;
            }

            if (!done.Add(persistentId.Value))
            {
                return;
            }

            Log.Info($"Entity {name} is sending notification to {notificationRecipient.name}...");
            notificationRecipient.Receive(notificationSpec.Notify());
        }
    }
}
