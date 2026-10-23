using System;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(IncrementalId))]
    public class Character : ShooterBehaviour, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        private IncrementalId incrementalId;

        [SerializeField] private Faction faction;
        public Faction Faction => faction;

        protected override void ShooterAwake()
        {
            incrementalId = GetComponent <IncrementalId>();
            if (faction == null)
            {
                Log.Error($"Entity {name} does not have a faction");
            }
        }

        public long Id => incrementalId.Value;

        public static Character FindById(long id, RegisterInactivePolicy inactivePolicy)
        {
            foreach (Character character in Registers.GetAll<Character>(inactivePolicy))
            {
                if (character.Id == id)
                {
                    return character;
                }
            }
            return null;
        }

        public static void ForEach(Action<Character> action, RegisterInactivePolicy inactivePolicy)
        {
            foreach (Character character in Registers.GetAll<Character>(inactivePolicy))
            {
                action(character);
            }
        }

        public DigestionPriority Priority => DigestionPriority.High;
        public string Digest(DigestionDetail detail)
        {
            return $"Character. Faction: {faction.PromptName}";
        }
    }
}
