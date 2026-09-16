using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Core.FractionsRelations;
using Shooter.Game.Core.Groups;
using Shooter.Game.Core.Saves;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.AI
{
    [RequireComponent(typeof(Character))]
    [RequireComponent(typeof(Nameable))]
    public class AICharacterRelation : MonoBehaviour, IDigestible, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] [Range(0, 10)] private float damageToReputationCoefficient = 1;

        [SerializeField] [Range(0, 100)] private int enemyThreshold;
        [SerializeField] [Range(0, 100)] private int friendThreshold = 90;

        private readonly Dictionary<long, int> amounts = new();

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
            return new SaveData()
            {
                Entries = amounts.Select(pair => new SaveData.Entry
                {
                    Id = pair.Key,
                    Amount = pair.Value
                }).ToList()
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.To<SaveData>();
            amounts.Clear();
            if (sd.Entries == null) return;
            foreach (SaveData.Entry entry in sd.Entries)
            {
                if (!amounts.TryAdd(entry.Id, entry.Amount))
                {
                    Log.Warn($"Save token contains multiple definitions of relation with {entry.Id}");
                }
            }
        }

        private Health health;
        private Character ownCharacter;

        public struct OnDamagedCallbackData
        {
            public int RelationDelta { get; set; }
            public int DamagePoints { get; set; }
            public DamageSpec DamageType { get; set; }
            public long AttackerId { get; set; }
        }
        public event Action<OnDamagedCallbackData> OnDamagedCallback;

        private void Awake()
        {
            ownCharacter = GetComponent<Character>();
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
        }

        public string Digest(DigestionDetail detail)
        {
            if (detail == DigestionDetail.Brief) return null;

            var sb = new StringBuilder();

            sb.Append("Current relations with other characters. ");
            sb.Append($"Thresholds. Enemies: <= {enemyThreshold}. Friends: >= {friendThreshold}. ");
            foreach (KeyValuePair<long, int> kvp in amounts)
            {
                sb.Append(kvp.Key + " : " + kvp.Value + " (" + Status(kvp.Value) + "). ");
            }

            sb.Append("The relation towards characters not listed here by fractions: ");
            FractionCatalog fractions = Catalogs.Of<FractionCatalog>();
            FractionRelationCatalog fractionRelations = Catalogs.Of<FractionRelationCatalog>();
            for (int i = 0; i < fractions.Count; i++)
            {
                Fraction targetFraction = fractions.At(i);
                sb.Append($"[{targetFraction.PromptName}: {fractionRelations.Amount(ownCharacter.Fraction, targetFraction)}], ");
            }

            return sb.ToString();
        }

        public DigestionPriority Priority => DigestionPriority.High;

        private void OnDamaged(double amount, Character attacker, DamageSpec type)
        {
            if (attacker == null) return;

            int delta = SetAmount(attacker, Math.Max(0, Amount(attacker) - (int)(damageToReputationCoefficient * amount)));
            if (delta != 0 && OnDamagedCallback != null)
            {
                OnDamagedCallback.Invoke(new OnDamagedCallbackData()
                {
                    RelationDelta = delta,
                    DamagePoints = (int)Math.Abs(amount),
                    DamageType = type,
                    AttackerId = attacker.Id
                });
            }
        }

        public int Amount(Character targetCharacter)
        {
            if (amounts.TryGetValue(targetCharacter.Id, out int amount))
            {
                return amount;
            }
            FractionRelationCatalog fractionRelations = Catalogs.Of<FractionRelationCatalog>();
            return fractionRelations.Amount(ownCharacter.Fraction, targetCharacter.Fraction);
        }

        public int SetAmount(Character targetCharacter, int amount)
        {
            int currentAmount = Amount(targetCharacter);

            Log.Info($"Entity {name} SetAmount request: character id {targetCharacter.Id} amount {currentAmount} -> {amount}");

            if (amount < 0 || amount > 100) return 0;

            if (amount == currentAmount) return 0;

            amounts[targetCharacter.Id] = amount;

            return amount - currentAmount;
        }

        public RelationshipStatus Status(Character targetCharacter)
        {
            return Status(Amount(targetCharacter));
        }

        private RelationshipStatus Status(int amount)
        {
            if (amount <= enemyThreshold) return RelationshipStatus.Enemy;

            if (amount >= friendThreshold) return RelationshipStatus.Friend;

            return RelationshipStatus.Neutral;
        }
    }
}
