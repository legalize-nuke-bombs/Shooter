using Unity.Netcode;

namespace Shooter.Game.Body.Notifying
{
    public class MailNotification : Notification
    {
        private long senderId;
        private string content;

        public MailNotification()
        {
            content = string.Empty;
        }

        public MailNotification(long senderId, string content)
        {
            this.senderId = senderId;
            this.content = content ?? string.Empty;
        }

        public long SenderId => senderId;

        public string Content => content;

        public override void NetworkSerialize<T>(BufferSerializer<T> serializer)
        {
            serializer.SerializeValue(ref senderId);
            serializer.SerializeValue(ref content);
        }
    }
}
