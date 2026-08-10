using System;
using System.Collections.Generic;
using System.Text;
using Shooter.Game.Body;
using Shooter.Game.Body.Notifying;
using Shooter.Game.Identity;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Relationship
{
    public class CharacterRelation : MonoBehaviour, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        private PersistentId ownId;

        private void Awake()
        {
            ownId = GetComponent<PersistentId>();
        }

        private readonly Dictionary<long, int> amounts = new Dictionary<long, int>();
        [SerializeField] [Range(0, 100)] private int defaultAmount = 50;
        // TODO логика стандартного отношения сильно упрощена, ее надо будет потом переделать

        public int Amount(long characterId)
        {
            return amounts.GetValueOrDefault(characterId, defaultAmount);
        }

        public void SetAmount(long characterId, int amount, string reason)
        {
            int currentAmount = Amount(characterId);

            Log.Info($"Entity {name} SetAmount request: character id {characterId} amount {currentAmount} -> {amount} reason {reason}");

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

            PersistentId target = Environment.Current.PersistentIds.Of(characterId);
            if (target == null) return;

            if (!target.TryGetComponent(out MainNotificationRecipient recipient)) return;

            recipient.Receive(new RelationChangedNotification(ownId.Value, before, after));
        }

        public void DecreaseAmount(long characterId, int amount, string reason)
        {
            SetAmount(characterId, Math.Max(0, Amount(characterId) - amount), reason);
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


