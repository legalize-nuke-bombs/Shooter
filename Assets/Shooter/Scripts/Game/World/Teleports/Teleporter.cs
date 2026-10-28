using System;
using UnityEngine;

namespace Shooter.Game
{
    public abstract class Teleporter : MonoBehaviour
    {
        public event Action<Character> Teleported;

        protected void Teleport(Character character, Movement movement, Vector3 destination)
        {
            movement.Teleport(destination);
            Teleported?.Invoke(character);
        }
    }
}
