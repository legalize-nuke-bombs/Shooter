using System;
using Shooter.Logging;
using Unity.Collections;
using Unity.Netcode;

namespace Shooter.Game.Packing
{
    public struct PackedBytes : INetworkSerializable, IEquatable<PackedBytes>
    {
        private static readonly Journal Log = Logs.Here();

        private FixedList64Bytes<byte> bytes;

        public bool IsEmpty => bytes.Length == 0;

        public static PackedBytes Of<TBase>(TBase value) where TBase : class, INetworkSerializable
        {
            var packed = new PackedBytes();
            if (value == null) return packed;

            using var writer = new FastBufferWriter(packed.bytes.Capacity, Allocator.Temp, 1024);
            writer.WriteNetworkSerializable(new Packed<TBase>(value));

            byte[] written = writer.ToArray();

            if (written.Length > packed.bytes.Capacity)
            {
                Log.Error($"A {value.GetType().Name} takes {written.Length} bytes of state, more than the {packed.bytes.Capacity} the network format holds");

                return default;
            }

            foreach (byte one in written) packed.bytes.Add(one);

            return packed;
        }

        public TBase Unpack<TBase>() where TBase : class, INetworkSerializable
        {
            if (IsEmpty) return null;

            var raw = new byte[bytes.Length];
            for (int index = 0; index < bytes.Length; index++) raw[index] = bytes[index];

            using var reader = new FastBufferReader(raw, Allocator.Temp);
            reader.ReadNetworkSerializable(out Packed<TBase> packed);

            return packed.Value;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            byte length = serializer.IsWriter ? (byte)bytes.Length : (byte)0;
            serializer.SerializeValue(ref length);

            if (serializer.IsReader)
            {
                bytes.Clear();

                for (int index = 0; index < length; index++)
                {
                    byte one = 0;
                    serializer.SerializeValue(ref one);
                    bytes.Add(one);
                }

                return;
            }

            for (int index = 0; index < length; index++)
            {
                byte one = bytes[index];
                serializer.SerializeValue(ref one);
            }
        }

        public bool Equals(PackedBytes other)
        {
            return bytes.Equals(other.bytes);
        }
    }
}
