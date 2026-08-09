namespace Shooter.Game.Body.Notifying
{
    public class RelationChangedNotification : Notification
    {
        public long ActorId { get; set; }

        public int Before { get; set; }

        public int After { get; set; }
    }
}
