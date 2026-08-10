using Unity.Netcode;

namespace Shooter.Game.Body.Notifying
{
    public class ItemsGivenNotification : Notification
    {
        private long actorId;
        private string itemSpecId;
        private int amount;

        public ItemsGivenNotification()
        {
            itemSpecId = string.Empty;
        }

        public ItemsGivenNotification(long actorId, string itemSpecId, int amount)
        {
            this.actorId = actorId;
            this.itemSpecId = itemSpecId ?? string.Empty;
            this.amount = amount;
        }

        public long ActorId => actorId;

        public string ItemSpecId => itemSpecId;

        public int Amount => amount;

        public override void NetworkSerialize<T>(BufferSerializer<T> serializer)
        {
            serializer.SerializeValue(ref actorId);
            serializer.SerializeValue(ref itemSpecId);
            serializer.SerializeValue(ref amount);
        }
    }
}
