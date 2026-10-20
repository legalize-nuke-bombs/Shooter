using System;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    public abstract class Trigger : ShooterNetworkBehaviour
    {
        public event Action<Character> Fired;

        protected void Fire(Character character)
        {
            Fired?.Invoke(character);
        }
    }
}
