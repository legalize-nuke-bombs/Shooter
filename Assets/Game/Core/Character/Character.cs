using System;
using UnityEngine;

namespace Shooter.Game.Core
{
    [RequireComponent(typeof(GameObjectRuntimeId))]
    public class Character : RegisteredBehaviour
    {
        private GameObjectRuntimeId id;

        protected override void Awake()
        {
            base.Awake();
            id = GetComponent<GameObjectRuntimeId>();
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
    }
}
