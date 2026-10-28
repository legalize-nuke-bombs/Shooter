using System;
using Unity.Netcode;

namespace Shooter.Game
{
    public abstract class Trigger : NetworkBehaviour
    {
        public event Action<Character> Fired;

        protected void Fire(Character character)
        {
            Fired?.Invoke(character);
        }
    }
}
