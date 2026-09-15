using System;
using Shooter.Game.Core.Groups;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core
{
    [RequireComponent(typeof(GameObjectRuntimeId))]
    public class Character : RegisteredBehaviour, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        private GameObjectRuntimeId id;

        [SerializeField] private Fraction fraction;
        public Fraction Fraction => fraction;

        protected override void Awake()
        {
            base.Awake();
            id = GetComponent<GameObjectRuntimeId>();
            if (fraction == null)
            {
                Log.Error($"Entity {name} does not have a group");
            }
        }

        public long Id => id.Value;

        public static Character Of(long id, Inactive gate)
        {
            foreach (Character character in Registers.Current.Of<Character>(gate))
            {
                if (character.Id == id)
                {
                    return character;
                }

            }
            return null;
        }

        public static void ForEach(Action<Character> action, Inactive gate)
        {
            foreach (Character character in Registers.Current.Of<Character>(gate))
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
