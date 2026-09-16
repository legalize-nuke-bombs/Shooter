using System;
using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Notifying
{
    [RequireComponent(typeof(Character))]
    public class NotificationRecipient : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        public event Action<NotificationSpec> Received;

        public void Receive(NotificationSpec spec)
        {
            if (spec == null) return;

            Log.Info($"Entity {name} received notification {spec.Key}");
            Received?.Invoke(spec);
        }
    }
}
