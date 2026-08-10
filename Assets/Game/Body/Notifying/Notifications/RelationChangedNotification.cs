using Unity.Netcode;

namespace Shooter.Game.Body.Notifying
{
    public class RelationChangedNotification : Notification
    {
        private long actorId;
        private int before;
        private int after;

        public RelationChangedNotification()
        {
        }

        public RelationChangedNotification(long actorId, int before, int after)
        {
            this.actorId = actorId;
            this.before = before;
            this.after = after;
        }

        public long ActorId => actorId;

        public int Before => before;

        public int After => after;

        public override void NetworkSerialize<T>(BufferSerializer<T> serializer)
        {
            serializer.SerializeValue(ref actorId);
            serializer.SerializeValue(ref before);
            serializer.SerializeValue(ref after);
        }
    }
}
