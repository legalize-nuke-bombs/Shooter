using Shooter.Game.Body.EarSounding;
using Shooter.Game.Body.Notifying;
using UnityEngine;

namespace Shooter.Client.Interface.Notifying
{
    public sealed class RelationChangedLine : NotificationLine<RelationChangedNotification>
    {
        [SerializeField] private Sprite improvedIcon;
        [SerializeField] private Sprite worsenedIcon;
        [SerializeField] private EarSoundSpec improvedSound;
        [SerializeField] private EarSoundSpec worsenedSound;

        protected override Sprite Icon(RelationChangedNotification notification) =>
            Improved(notification) ? improvedIcon : worsenedIcon;

        protected override string Title(RelationChangedNotification notification) =>
            Improved(notification) ? "Отношение улучшилось" : "Отношение ухудшилось";

        protected override long Actor(RelationChangedNotification notification) => notification.ActorId;

        protected override EarSoundSpec Sound(RelationChangedNotification notification) =>
            Improved(notification) ? improvedSound : worsenedSound;

        private static bool Improved(RelationChangedNotification notification) => notification.After > notification.Before;
    }
}
