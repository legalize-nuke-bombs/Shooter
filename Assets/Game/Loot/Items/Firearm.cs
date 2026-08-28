using System;
using Shooter.Game.Core.Saves;
using Unity.Netcode;

namespace Shooter.Game.Loot
{
    public class Firearm : UniqueItem
    {
        private int magazine;

        public Firearm(string specId) : base(specId)
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
            Touch();

            return true;
        }

        public int Reload(int rounds, int size)
        {
            int taken = Math.Min(rounds, size - magazine);
            if (taken <= 0) return 0;

            magazine += taken;
            Touch();

            return taken;
        }

        public override void NetworkSerialize<T>(BufferSerializer<T> serializer)
        {
            serializer.SerializeValue(ref magazine);
        }
    }
}
