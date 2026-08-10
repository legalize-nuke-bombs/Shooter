using System;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Unity.Collections;
using Unity.Netcode;

namespace Shooter.Game.Notifying
{
    public struct Notification : INetworkSerializable
    {
        private FixedString32Bytes spec;
        private FixedString32Bytes icon;
        private FixedString32Bytes sound;
        private Arg[] args;

        public Notification(FixedString32Bytes spec)
        {
            this.spec = spec;
            icon = default;
            sound = default;
            args = Array.Empty<Arg>();
        }

        public FixedString32Bytes Spec => spec;

        public FixedString32Bytes Icon => icon;

        public FixedString32Bytes Sound => sound;

        public bool IsEmpty => spec.IsEmpty;

        public string Of(string name)
        {
            if (args == null || string.IsNullOrEmpty(name)) return null;

            foreach (Arg arg in args)
            {
                if (arg.Name == name) return arg.Value;
            }

            return null;
        }

        public Notification With(string name, string value)
        {
            return Holding(new Arg(name, value));
        }

        public Notification With(string name, long value)
        {
            return Holding(new Arg(name, value.ToString()));
        }

        public Notification Under(IconSpec own)
        {
            Notification copy = this;
            copy.icon = own == null ? default : own.Id;

            return copy;
        }

        public Notification Along(EarSoundSpec own)
        {
            Notification copy = this;
            copy.sound = own == null ? default : own.Id;

            return copy;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref spec);
            serializer.SerializeValue(ref icon);
            serializer.SerializeValue(ref sound);

            byte count = serializer.IsWriter ? (byte)(args == null ? 0 : args.Length) : (byte)0;
            serializer.SerializeValue(ref count);

            if (serializer.IsReader) args = new Arg[count];

            for (int index = 0; index < count; index++)
            {
                Arg arg = args[index];
                arg.NetworkSerialize(serializer);
                args[index] = arg;
            }
        }

        private Notification Holding(Arg arg)
        {
            int length = args == null ? 0 : args.Length;

            var grown = new Arg[length + 1];
            for (int index = 0; index < length; index++) grown[index] = args[index];
            grown[length] = arg;

            Notification copy = this;
            copy.args = grown;

            return copy;
        }
    }
}
