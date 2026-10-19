using System.Collections.Generic;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Fraction Relation Catalog", fileName = "FractionRelationCatalog")]
    public class FractionRelationCatalog : Catalog<FractionRelation>
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private readonly Dictionary<string, int> defaultAmountsByFractionPair = new();

        [SerializeField] private int defaultAmount = 50;

        protected override void OnEnable()
        {
            base.OnEnable();
            defaultAmountsByFractionPair.Clear();

            for (int i = 0; i < Count; i++)
            {
                FractionRelation fractionRelation = GetAssetAt(i);
                string fractionPairKey = GetFractionPairKey(fractionRelation.Fraction1.Id, fractionRelation.Fraction2.Id);
                if (!defaultAmountsByFractionPair.TryAdd(fractionPairKey, fractionRelation.DefaultAmount))
                {
                    Log.Error($"Fraction relation {fractionPairKey} already has been set");
                }
            }
        }

        public int GetDefaultAmount(Fraction fraction1, Fraction fraction2)
        {
            string fractionPairKey = GetFractionPairKey(fraction1.Id, fraction2.Id);
            if (defaultAmountsByFractionPair.TryGetValue(fractionPairKey, out int amount))
            {
                return amount;
            }
            Log.Error($"Failed to find relation value for {fractionPairKey}, falling back to {defaultAmount}");
            return defaultAmount;
        }

        private static string GetFractionPairKey(string fractionId1, string fractionId2)
        {
            if (string.CompareOrdinal(fractionId1, fractionId2) > 0)
            {
                (fractionId1, fractionId2) = (fractionId2, fractionId1);
            }
            return fractionId1 + "-" + fractionId2;
        }
    }
}
