using System;
using System.Collections.Generic;
using System.Text;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Notifying;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.AI
{
    [RequireComponent(typeof(Character))]
    [RequireComponent(typeof(Nameable))]
    public class AICharacterRelation : MonoBehaviour, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] [Range(0, 100)]
        private int defaultAmount = 50; // TODO логика стандартного отношения сильно упрощена, ее надо будет потом переделать

        [SerializeField] [Range(0, 10)] private float damageToReputationCoefficient = 1;

        [SerializeField] private NotificationSpec improved;
        [SerializeField] private NotificationSpec worsened;

        [SerializeField] [Range(0, 100)] private int enemyThreshold;
        [SerializeField] [Range(0, 100)] private int friendThreshold = 90;

        private readonly Dictionary<long, int> amounts = new();
        private Health health;

        private Character ownId;
        private Nameable ownNameable;
        public float DamageToReputationCoefficient => damageToReputationCoefficient;

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
            ownId = GetComponent<Character>();
            ownNameable = GetComponent<Nameable>();
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
                sb.Append(kvp.Key + " : " + kvp.Value + " (" + Status(kvp.Value) + "). ");
            sb.Append($"The relation towards characters not listed here is the standard {defaultAmount}.");

            return sb.ToString();
        }

        public DigestionPriority Priority => DigestionPriority.High;

        private void OnDamaged(double amount, long? attackerId, DamageSpec type)
        {
            if (attackerId == null) return;

            int delta = SetAmount(attackerId.Value, Math.Max(0, Amount(attackerId.Value) - (int)(damageToReputationCoefficient * amount)));
            if (delta != 0 && OnDamagedCallback != null)
            {
                OnDamagedCallback.Invoke(new OnDamagedCallbackData()
                {
                    RelationDelta = delta,
                    DamagePoints = (int)Math.Abs(amount),
                    DamageType = type,
                    AttackerId = attackerId.Value
                });
            }
        }

        public void SetDamageToReputationCoefficient(float amount)
        {
            if (amount < 0 || amount > 10) throw new ArgumentException($"Amount must be 0 <= ? <= 10, got {amount}");
            damageToReputationCoefficient = amount;
        }

        public int Amount(long characterId)
        {
            return amounts.GetValueOrDefault(characterId, defaultAmount);
        }

        public int SetAmount(long characterId, int amount)
        {
            int currentAmount = Amount(characterId);

            Log.Info($"Entity {name} SetAmount request: character id {characterId} amount {currentAmount} -> {amount}");

            if (amount < 0 || amount > 100) return 0;

            if (amount == currentAmount) return 0;

            amounts[characterId] = amount;

            Notify(characterId, currentAmount, amount);
            return amount - currentAmount;
        }

        private void Notify(long characterId, int before, int after)
        {
            Character target = Registers.Current.Of<Character>().Of(characterId);
            if (target == null)
            {
                Log.Warn($"Entity {name} failed to notify character {characterId}: not found");
                return;
            }

            if (!target.TryGetComponent(out MainNotificationRecipient recipient))
            {
                Log.Warn($"Entity {name} failed to notify character {characterId}: not a notification recipient");
            }

            NotificationSpec spec = after > before ? improved : worsened;

            if (spec == null)
            {
                Log.Warn($"Entity {name} has no notification for an attitude that went {before} -> {after}, the change goes unnoticed");
                return;
            }

            recipient.Receive(spec.Notify()
                .With("actorId", ownId.Value)
                .With(ownNameable.NamedAs("actorName"))
                .With("before", before)
                .With("after", after));
        }

        public RelationshipStatus Status(long characterId)
        {
            return Status(Amount(characterId));
        }

        private RelationshipStatus Status(int amount)
        {
            if (amount <= enemyThreshold) return RelationshipStatus.Enemy;

            if (amount >= friendThreshold) return RelationshipStatus.Friend;

            return RelationshipStatus.Neutral;
        }
    }
}
