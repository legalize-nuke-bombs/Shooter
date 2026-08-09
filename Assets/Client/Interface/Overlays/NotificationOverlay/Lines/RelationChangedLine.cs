using Shooter.Game.Body.EarSounding;
using Shooter.Game.Body.Notifying;
using UnityEngine;

namespace Shooter.Client.Interface.Overlays
{
    public sealed class RelationChangedLine : NotificationLine<RelationChangedNotification>
    {
        private const string SoundId = "items_given";

        protected override Sprite Icon(RelationChangedNotification notification) => null;

        protected override string Title(RelationChangedNotification notification) =>
            notification.After > notification.Before ? "Отношение улучшилось" : "Отношение ухудшилось";

        protected override long Actor(RelationChangedNotification notification) => notification.ActorId;

        protected override EarSoundSpec Sound(RelationChangedNotification notification) => Sounds == null ? null : Sounds.Of(SoundId);
    }
}
