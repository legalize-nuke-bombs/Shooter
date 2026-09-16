using System;
using Unity.Netcode;

namespace Shooter.Game.Speech
{
    public struct Line : INetworkSerializable
    {
        public long AuthorId;
        public string Content;
        public DateTime Time;
        public bool Spoken;

        public static Line Of(Message message)
        {
            return new Line
            {
                AuthorId = message.AuthorId,
                Content = message.Content,
                Time = message.Time,
                Spoken = message.Spoken
            };
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            long ticks = Time.Ticks;

            serializer.SerializeValue(ref AuthorId);
            serializer.SerializeValue(ref Content);
            serializer.SerializeValue(ref ticks);
            serializer.SerializeValue(ref Spoken);

            if (serializer.IsReader) Time = new DateTime(ticks);
        }
    }
}
