namespace Shooter.Game.Body.Notifying
{
    public interface IChildNotificationRecipient
    {
        void OnReceive(Notification notification);
    }
}
