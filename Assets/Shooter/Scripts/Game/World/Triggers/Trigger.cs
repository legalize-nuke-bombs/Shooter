using System;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public abstract class Trigger : NetworkBehaviour
    {
        public event Action<Character> Fired;

        protected void Fire(Character character)
        {
            Fired?.Invoke(character);
        }
    }
}
