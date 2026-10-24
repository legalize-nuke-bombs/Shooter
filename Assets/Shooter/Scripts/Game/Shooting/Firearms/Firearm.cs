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
            magazine = content.ToObject<SaveData>().Magazine;
        }

        public int Magazine => magazine;

        public bool TrySpendRound()
        {
            if (magazine == 0) return false;

            magazine--;
            RaiseChanged();

            return true;
        }

        public int LoadRounds(int availableRounds, int magazineSize)
        {
            int loadedRounds = Math.Min(availableRounds, magazineSize - magazine);
            if (loadedRounds <= 0) return 0;

            magazine += loadedRounds;
            RaiseChanged();

            return loadedRounds;
        }

        public override void NetworkSerialize<T>(BufferSerializer<T> serializer)
        {
            serializer.SerializeValue(ref magazine);
        }
    }
}
