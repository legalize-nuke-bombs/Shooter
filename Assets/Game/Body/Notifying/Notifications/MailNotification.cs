namespace Shooter.Game.Body.Notifying
{
    public class MailNotification : Notification
    {
        public long SenderId { get; set; }

        public string Content { get; set; }
    }
}
