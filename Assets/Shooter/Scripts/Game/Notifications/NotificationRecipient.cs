using System;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Character))]
    public class NotificationRecipient : MonoBehaviour
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        public event Action<NotificationAsset> Received;

        public void Receive(NotificationAsset asset)
        {
            if (asset == null) return;

            Log.Info($"Entity {name} received notification {asset.Id}");
            Received?.Invoke(asset);
        }
    }
}
