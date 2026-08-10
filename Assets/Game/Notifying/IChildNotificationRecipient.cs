namespace Shooter.Game.Notifying
{
    public interface IChildNotificationRecipient
    {
        void OnReceive(Notification notification);
    }
}
