using System;
using UnityEngine;

namespace Shooter.Game.Core
{
    [RequireComponent(typeof(GameObjectRuntimeId))]
    public class Character : RegisteredBehaviour
    {
        private GameObjectRuntimeId id;

        private void Awake()
        {
            id = GetComponent<GameObjectRuntimeId>();
        }

        public long Id => id.Value;

        public static Character Of(long id)
        {
            foreach (Character character in Registers.Current.Of<Character>())
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
