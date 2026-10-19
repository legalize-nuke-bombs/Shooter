using System;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(CharacterId))]
    public class Character : ShooterBehaviour, IDigestible
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private CharacterId characterId;

        [SerializeField] private Fraction fraction;
        public Fraction Fraction => fraction;

        protected override void ShooterAwake()
        {
            characterId = GetComponent<CharacterId>();
            if (fraction == null)
            {
                Log.Error($"Entity {name} does not have a fraction");
            }
        }

        public long Id => characterId.Value;

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
            return $"Character. Fraction: {fraction.PromptName}";
        }
    }
}
