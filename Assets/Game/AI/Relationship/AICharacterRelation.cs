using System;
using System.Collections.Generic;
using System.Text;
using Shooter.Game.Body;
using Shooter.Game.Notifying;
using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.AI
{
    public class AICharacterRelation : MonoBehaviour, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        private PersistentId ownId;
        private Nameable ownNameable;
        private Health health;

        private void Awake()
        {
            ownId = this.Find<PersistentId>();
            ownNameable = this.Find<Nameable>();
            health = this.Find<Health>();
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
        }

        private void OnDamaged(double amount, long? attackerId, DamageSpec type)
        {
            if (!type.Reputational || attackerId == null) return;

            SetAmount(attackerId.Value, Math.Max(0, Amount(attackerId.Value) - (int)(damageToReputationCoefficient * amount)));
        }

        private readonly Dictionary<long, int> amounts = new Dictionary<long, int>();

        [SerializeField] [Range(0, 100)] private int defaultAmount = 50; // TODO логика стандартного отношения сильно упрощена, ее надо будет потом переделать

        [SerializeField] [Range(0, 10)] private float damageToReputationCoefficient = 1;
        public float DamageToReputationCoefficient => damageToReputationCoefficient;
        public void SetDamageToReputationCoefficient(float amount)
        {
            if (amount < 0 || amount > 10)
            {
                throw new ArgumentException($"Amount must be 0 <= ? <= 10, got {amount}");
            }
            damageToReputationCoefficient = amount;
        }

        [SerializeField] private NotificationSpec improved;
        [SerializeField] private NotificationSpec worsened;

        public int Amount(long characterId)
        {
            return amounts.GetValueOrDefault(characterId, defaultAmount);
        }

        public void SetAmount(long characterId, int amount)
        {
            int currentAmount = Amount(characterId);

            Log.Info($"Entity {name} SetAmount request: character id {characterId} amount {currentAmount} -> {amount}");

            if (amount < 0 || amount > 100)
            {
                throw new ArgumentException("Amount must be between 0 and 100");
            }

            if (amount == currentAmount)
            {
                return;
            }

            amounts[characterId] = amount;

            Notify(characterId, currentAmount, amount);
        }

        private void Notify(long characterId, int before, int after)
        {
            if (ownId == null) return;

            PersistentId target = Registers.Current.Of<PersistentId>().Of(characterId);
            if (target == null) return;

            if (!target.TryGetComponent(out MainNotificationRecipient recipient)) return;

            NotificationSpec spec = after > before ? improved : worsened;

            if (spec == null)
            {
                Log.Warn($"Entity {name} has no notification for an attitude that went {before} -> {after}, the change goes unnoticed");
                return;
            }

            recipient.Receive(spec.Notify()
                .With("actorId", ownId.Value)
                .With(ownNameable == null ? new Arg("actorName", string.Empty) : ownNameable.NamedAs("actorName"))
                .With("before", before)
                .With("after", after));
        }

        [SerializeField] [Range(0, 100)] private int enemyThreshold = 0;
        [SerializeField] [Range(0, 100)] private int friendThreshold = 90;

        public RelationshipStatus Status(long characterId)
        {
            return Status(Amount(characterId));
        }

        private RelationshipStatus Status(int amount)
        {
            if (amount <= enemyThreshold)
            {
                return RelationshipStatus.Enemy;
            }

            if (amount >= friendThreshold)
            {
                return RelationshipStatus.Friend;
            }

            return RelationshipStatus.Neutral;
        }

        public string Digest(DigestionDetail detail)
        {
            if (detail == DigestionDetail.Brief)
            {
                return null;
            }

            var sb = new StringBuilder();

            sb.Append("Current relations with other characters. ");
            sb.Append($"Thresholds. Enemies: <= {enemyThreshold}. Friends: >= {friendThreshold}. ");
            foreach (var kvp in amounts)
            {
                sb.Append(kvp.Key + " : " + kvp.Value + " (" + Status(kvp.Value) + "). ");
            }
            sb.Append($"The relation towards characters not listed here is the standard {defaultAmount}.");

            return sb.ToString();
        }

        public DigestionPriority Priority => DigestionPriority.High;
    }
}


