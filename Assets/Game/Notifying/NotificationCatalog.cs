using UnityEngine;
using Shooter.Game.Core;

namespace Shooter.Game.Notifying
{
    [CreateAssetMenu(menuName = "Shooter/Notification Catalog", fileName = "NotificationCatalog")]
    public class NotificationCatalog : Catalog<NotificationSpec>
    {
    }
}
