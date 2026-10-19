using System;
using Shooter.Infrastructure;
using Unity.Netcode;

namespace Shooter.Game
{
    public class Firearm : UniqueItem
    {
        private int roundsInMagazine;

        public Firearm(string assetId) : base(assetId)
        {
        }

        private struct SaveData
        {
            public int Magazine { get; set; }
        }
        public override object SaveObject()
        {
            return new SaveData { Magazine = roundsInMagazine };
        }
        public override void LoadObject(SaveToken content)
        {
            roundsInMagazine = content.ToObject<SaveData>().Magazine;
        }

        public int RoundsInMagazine => roundsInMagazine;

        public bool TrySpendRound()
        {
            if (roundsInMagazine == 0) return false;

            roundsInMagazine--;
            RaiseChanged();

            return true;
        }

        public int LoadRounds(int availableRounds, int magazineSize)
        {
            int loadedRounds = Math.Min(availableRounds, magazineSize - roundsInMagazine);
            if (loadedRounds <= 0) return 0;

            roundsInMagazine += loadedRounds;
            RaiseChanged();

            return loadedRounds;
        }

        public override void NetworkSerialize<T>(BufferSerializer<T> serializer)
        {
            serializer.SerializeValue(ref roundsInMagazine);
        }
    }
}
