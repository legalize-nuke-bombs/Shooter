using System;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public abstract class Trigger : ShooterNetworkComponent
    {
        public event Action<Character> Fired;

        protected void Fire(Character character)
        {
            Fired?.Invoke(character);
        }
    }
}
