using System;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public abstract class Teleporter : ShooterBehaviour
    {
        public event Action<Character> Teleported;

        protected void Teleport(Character character, Movement movement, Vector3 destination)
        {
            movement.Teleport(destination);
            Teleported?.Invoke(character);
        }
    }
}
