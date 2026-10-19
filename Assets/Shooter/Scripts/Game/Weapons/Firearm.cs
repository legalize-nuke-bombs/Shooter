using System;
using Shooter.Infrastructure;
using Unity.Netcode;

namespace Shooter.Game
{
    public class Firearm : UniqueItem
    {
        private int magazine;

        public Firearm(string assetId) : base(assetId)
        {
        }

        private struct SaveData
        {
            public int Magazine { get; set; }
        }
        public override object SaveObject()
        {
            return new SaveData { Magazine = magazine };
        }
        public override void LoadObject(SaveToken content)
        {
            magazine = content.To<SaveData>().Magazine;
        }

        public int Magazine => magazine;

        public bool Spend()
        {
            if (magazine == 0) return false;

            magazine--;
            OnChanged();

            return true;
        }

        public int Reload(int rounds, int size)
        {
            int taken = Math.Min(rounds, size - magazine);
            if (taken <= 0) return 0;

            magazine += taken;
            OnChanged();

            return taken;
        }

        public override void NetworkSerialize<T>(BufferSerializer<T> serializer)
        {
            serializer.SerializeValue(ref magazine);
        }
    }
}
