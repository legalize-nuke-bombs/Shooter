using Shooter.Game.Core;
using UnityEngine;

namespace Shooter.Game.Notifying
{
    [CreateAssetMenu(menuName = "Shooter/Notification Catalog", fileName = "NotificationCatalog")]
    public class NotificationCatalog : Catalog<NotificationSpec>
    {
    }
}
