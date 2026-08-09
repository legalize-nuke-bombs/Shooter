namespace Shooter.Game.Body.Notifying
{
    public class ItemsGivenNotification : Notification
    {
        public long ActorId { get; set; }

        public string ItemSpecId { get; set; }

        public int Amount { get; set; }
    }
}
