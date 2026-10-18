using System;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Character))]
    public class NotificationRecipient : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        public event Action<NotificationSpec> Received;

        public void Receive(NotificationSpec spec)
        {
            if (spec == null) return;

            Log.Info($"Entity {name} received notification {spec.Id}");
            Received?.Invoke(spec);
        }
    }
}
