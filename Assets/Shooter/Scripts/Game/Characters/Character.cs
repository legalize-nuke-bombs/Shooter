using System;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(CharacterId))]
    public class Character : ShooterBehaviour, IDigestible
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private CharacterId id;

        [SerializeField] private Fraction fraction;
        public Fraction Fraction => fraction;

        protected override void ShooterAwake()
        {
            id = GetComponent<CharacterId>();
            if (fraction == null)
            {
                Log.Error($"Entity {name} does not have a group");
            }
        }

        public long Id => id.Value;

        public static Character Of(long id, RegisterInactivePolicy gate)
        {
            foreach (Character character in Registers.GetAll<Character>(gate))
            {
                if (character.Id == id)
                {
                    return character;
                }

            }
            return null;
        }

        public static void ForEach(Action<Character> action, RegisterInactivePolicy gate)
        {
            foreach (Character character in Registers.GetAll<Character>(gate))
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
