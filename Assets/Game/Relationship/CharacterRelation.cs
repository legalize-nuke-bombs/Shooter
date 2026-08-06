using System;
using System.Collections.Generic;
using System.Text;
using Shooter.Game.Body;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Relationship
{
    public class CharacterRelation : MonoBehaviour, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<string, int> amounts = new Dictionary<string, int>();
        [SerializeField] [Range(0, 100)] private int defaultAmount = 50;
        // TODO логика стандартного отношения сильно упрощена, ее надо будет потом переделать

        private readonly Queue<RelationChangelog> changelog = new Queue<RelationChangelog>();
        [SerializeField] private int maxChangelogSize = 20;

        public int Amount(string characterName)
        {
            return amounts.GetValueOrDefault(characterName, defaultAmount);
        }

        public void SetAmount(string characterName, int amount, string reason)
        {
            int currentAmount = Amount(characterName);

            Log.Info("SetAmount request: character name {} {} -> {} reason {}", characterName, currentAmount, amount, reason);

            if (amount < 0 || amount > 100)
            {
                throw new ArgumentException("Amount must be between 0 and 100");
            }

            if (amount == currentAmount)
            {
                return;
            }

            changelog.Enqueue(new RelationChangelog()
            {
                    Time = Environment.Current.Clock.DateTime(),
                    Name = characterName,
                    From = currentAmount,
                    To = amount,
                    Reason = reason
            });
            while (changelog.Count > maxChangelogSize)
            {
                changelog.Dequeue();
            }

            amounts[characterName] = amount;
        }

        public void DecreaseAmount(string characterName, int amount, string reason)
        {
            SetAmount(characterName, Math.Max(0, Amount(characterName) - amount), reason);
        }

        [SerializeField] [Range(0, 100)] private int enemyThreshold = 0;
        [SerializeField] [Range(0, 100)] private int friendThreshold = 90;

        public RelationshipStatus Status(string characterName)
        {
            return Status(Amount(characterName));
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
            sb.Append($"The relation towards characters not listed here is the standard {defaultAmount}.\n");

            sb.Append("Relations with other characters changelog. ");
            foreach (RelationChangelog rc in changelog)
            {
                sb.Append($"[{rc.Time}] Relation to {rc.Name} {rc.From} -> {rc.To} reason: {rc.Reason}. ");
            }

            return sb.ToString();
        }

        public DigestionPriority Priority => DigestionPriority.High;
    }
}


