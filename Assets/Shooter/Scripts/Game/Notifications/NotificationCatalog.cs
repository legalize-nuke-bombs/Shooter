using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Notification Catalog", fileName = "NotificationCatalog")]
    public class NotificationCatalog : Catalog<NotificationSpec>
    {
    }
}
