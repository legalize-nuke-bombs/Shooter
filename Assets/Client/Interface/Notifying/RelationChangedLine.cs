using Shooter.Game.Body.Notifying;

namespace Shooter.Client.Interface.Notifying
{
    public sealed class RelationChangedLine : NotificationLine<RelationChangedNotification>
    {
        protected override string Title(RelationChangedNotification notification) =>
            notification.After > notification.Before ? "Отношение улучшилось" : "Отношение ухудшилось";

        protected override long Actor(RelationChangedNotification notification) => notification.ActorId;
    }
}
