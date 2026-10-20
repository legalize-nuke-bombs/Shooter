using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Character))]
    [RequireComponent(typeof(Nameable))]
    public class AICharacterRelation : MonoBehaviour, IDigestible, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] [Range(0, 10)] private float damageToReputationCoefficient = 1;

        [SerializeField] [Range(0, 100)] private int enemyThreshold;
        [SerializeField] [Range(0, 100)] private int friendThreshold = 90;

        private readonly Dictionary<long, int> amountsByCharacterId = new();

        public string ComponentKey => "AICharacterRelation";

        private struct SaveData
        {
            public struct Entry
            {
                public long Id { get; set; }
                public int Amount { get; set; }
            }
            public List<Entry> Entries { get; set; }
        }

        public object SaveObject()
        {
            return new SaveData
            {
                Entries = amountsByCharacterId.Select(pair => new SaveData.Entry
                {
                    Id = pair.Key,
                    Amount = pair.Value
                }).ToList()
            };
        }

        public void LoadObject(SaveToken content)
        {
            SaveData saveData = content.ToObject<SaveData>();
            amountsByCharacterId.Clear();
            if (saveData.Entries == null) return;
            foreach (SaveData.Entry entry in saveData.Entries)
            {
                if (!amountsByCharacterId.TryAdd(entry.Id, entry.Amount))
                {
                    Log.Warn($"Save token contains multiple definitions of relation with {entry.Id}");
                }
            }
        }

        private Health health;
        private Character ownCharacter;

        public struct RelationWorsenedByDamageData
        {
            public int RelationDelta { get; set; }
            public int DamagePoints { get; set; }
            public DamageAsset DamageType { get; set; }
            public long AttackerId { get; set; }
        }

        public event Action<RelationWorsenedByDamageData> RelationWorsenedByDamage;

        private void Awake()
        {
            ownCharacter = GetComponent<Character>();
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            health.Damaged += HandleDamaged;
        }

        private void OnDisable()
        {
            health.Damaged -= HandleDamaged;
        }

        public string Digest(DigestionDetail detail)
        {
            if (detail == DigestionDetail.Brief) return null;

            StringBuilder digest = new();

            digest.Append("Current relations with other characters. ");
            digest.Append($"Thresholds. Enemies: <= {enemyThreshold}. Friends: >= {friendThreshold}. ");
            foreach (KeyValuePair<long, int> amountByCharacterId in amountsByCharacterId)
            {
                digest.Append(amountByCharacterId.Key + " : " + amountByCharacterId.Value + " (" + GetStatus(amountByCharacterId.Value) + "). ");
            }

            digest.Append("The relation towards characters not listed here by factions: ");
            FactionCatalog factionCatalog = Catalogs.GetCatalog<FactionCatalog>();
            FactionRelationCatalog factionRelationCatalog = Catalogs.GetCatalog<FactionRelationCatalog>();
            for (int i = 0; i < factionCatalog.Count; i++)
            {
                Faction targetFaction = factionCatalog.GetAssetAt(i);
                digest.Append($"[{targetFaction.PromptName}: {factionRelationCatalog.GetDefaultAmount(ownCharacter.Faction, targetFaction)}], ");
            }

            return digest.ToString();
        }

        public DigestionPriority Priority => DigestionPriority.High;

        private void HandleDamaged(double amount, Character attacker, DamageAsset type)
        {
            if (attacker == null) return;

            int delta = SetAmount(attacker, GetAmount(attacker) - (int)(damageToReputationCoefficient * amount));
            if (delta != 0 && RelationWorsenedByDamage != null)
            {
                RelationWorsenedByDamage.Invoke(new RelationWorsenedByDamageData
                {
                    RelationDelta = delta,
                    DamagePoints = (int)Math.Abs(amount),
                    DamageType = type,
                    AttackerId = attacker.Id
                });
            }
        }

        public int GetAmount(Character targetCharacter)
        {
            if (amountsByCharacterId.TryGetValue(targetCharacter.Id, out int amount))
            {
                return amount;
            }
            FactionRelationCatalog factionRelationCatalog = Catalogs.GetCatalog<FactionRelationCatalog>();
            return factionRelationCatalog.GetDefaultAmount(ownCharacter.Faction, targetCharacter.Faction);
        }

        public int SetAmount(Character targetCharacter, int amount)
        {
            int currentAmount = GetAmount(targetCharacter);
            int clampedAmount = Math.Clamp(amount, 0, 100);

            Log.Info($"Entity {name} SetAmount request: character id {targetCharacter.Id} amount {currentAmount} -> {amount}");

            if (clampedAmount == currentAmount) return 0;

            amountsByCharacterId[targetCharacter.Id] = clampedAmount;

            return clampedAmount - currentAmount;
        }

        public RelationStatus GetStatus(Character targetCharacter)
        {
            return GetStatus(GetAmount(targetCharacter));
        }

        private RelationStatus GetStatus(int amount)
        {
            if (amount <= enemyThreshold) return RelationStatus.Enemy;

            if (amount >= friendThreshold) return RelationStatus.Friend;

            return RelationStatus.Neutral;
        }
    }
}
