using System;
using System.Collections.Generic;
using Shooter.Game.Core.Groups;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core.FractionsRelations
{
    [CreateAssetMenu(menuName = "Shooter/Fraction Relation Catalog", fileName = "FractionRelationCatalog")]
    public class FractionRelationCatalog : Catalog<FractionRelation>
    {
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<string, int> byFractions = new Dictionary<string, int>();

        [SerializeField] private int defaultAmount = 50;

        protected override void OnEnable()
        {
            base.OnEnable();

            for (int i = 0; i < Count; i++)
            {
                FractionRelation fractionRelation = At(i);
                string key = KeyOf(fractionRelation.Fraction1.Id.ToString(), fractionRelation.Fraction2.Id.ToString());
                if (!byFractions.TryAdd(key, fractionRelation.DefaultAmount))
                {
                    Log.Error($"Fraction relation {key} already has been set");
                }
            }
        }

        public int Amount(Fraction fraction1, Fraction fraction2)
        {
            string key = KeyOf(fraction1.Id.ToString(), fraction2.Id.ToString());
            if (byFractions.TryGetValue(key, out int result))
            {
                return result;
            }
            Log.Error($"Failed to find relation value for {key}, falling back to {defaultAmount}");
            return defaultAmount;
        }

        private static string KeyOf(string g1, string g2)
        {
            if (String.CompareOrdinal(g1, g2) > 0)
            {
                (g1, g2) = (g2, g1);
            }
            return g1 + "-" + g2;
        }
    }
}
