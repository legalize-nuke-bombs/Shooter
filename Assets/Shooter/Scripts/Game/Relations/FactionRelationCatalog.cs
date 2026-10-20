using System.Collections.Generic;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Faction Relation Catalog", fileName = "FactionRelationCatalog")]
    public class FactionRelationCatalog : Catalog<FactionRelation>
    {
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<string, int> defaultAmountsByFactionPair = new();

        [SerializeField] private int defaultAmount = 50;

        protected override void OnEnable()
        {
            base.OnEnable();
            defaultAmountsByFactionPair.Clear();

            for (int i = 0; i < Count; i++)
            {
                FactionRelation factionRelation = GetAssetAt(i);
                string factionPairKey = GetFactionPairKey(factionRelation.Faction1.Id, factionRelation.Faction2.Id);
                if (!defaultAmountsByFactionPair.TryAdd(factionPairKey, factionRelation.DefaultAmount))
                {
                    Log.Error($"Faction relation {factionPairKey} already has been set");
                }
            }
        }

        public int GetDefaultAmount(Faction faction1, Faction faction2)
        {
            string factionPairKey = GetFactionPairKey(faction1.Id, faction2.Id);
            if (defaultAmountsByFactionPair.TryGetValue(factionPairKey, out int amount))
            {
                return amount;
            }
            Log.Error($"Failed to find relation value for {factionPairKey}, falling back to {defaultAmount}");
            return defaultAmount;
        }

        private static string GetFactionPairKey(string factionId1, string factionId2)
        {
            if (string.CompareOrdinal(factionId1, factionId2) > 0)
            {
                (factionId1, factionId2) = (factionId2, factionId1);
            }
            return factionId1 + "-" + factionId2;
        }
    }
}
