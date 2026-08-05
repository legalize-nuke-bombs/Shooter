using System;
using System.Collections.Generic;
using System.Text;
using Shooter.Game.Body;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Relationship
{
    public class CharacterRelation : NetworkBehaviour, IDigestible
    {
        private readonly NetworkVariable<Dictionary<string, int>> amounts = new NetworkVariable<Dictionary<string, int>>(new Dictionary<string, int>());
        [SerializeField] [Range(0, 100)] private int defaultAmount = 50;
        // TODO логика стандартного отношения сильно упрощена, ее надо будет потом переделать

        public int Amount(string playerName)
        {
            return amounts.Value.GetValueOrDefault(playerName, defaultAmount);
        }

        public void SetAmount(string playerName, int amount)
        {
            if (amount < 0 || amount > 100)
            {
                throw new ArgumentException("Amount must be between 0 and 100");
            }
            amounts.Value[playerName] = amount;
        }

        [SerializeField] [Range(0, 100)] private int enemyThreshold = 0;
        [SerializeField] [Range(0, 100)] private int friendThreshold = 90;

        public RelationshipStatus Status(string playerName)
        {
            return Status(Amount(playerName));
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

            sb.Append("Relations with other characters. ");
            sb.Append($"Thresholds. Enemies: <= {enemyThreshold}. Friends: >= {friendThreshold}. ");

            foreach (var kvp in amounts.Value)
            {
                sb.Append(kvp.Key + " : " + kvp.Value + " (" + Status(kvp.Value) + "). ");
            }

            sb.Append($"The relation towards characters not listed here is the standard {defaultAmount}.");

            return sb.ToString();
        }

        public DigestionPriority Priority => DigestionPriority.High;
    }
}


