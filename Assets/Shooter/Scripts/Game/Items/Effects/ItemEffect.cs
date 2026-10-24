using UnityEngine;

namespace Shooter.Game
{
    public abstract class ItemEffect : ScriptableObject
    {
        public abstract void ApplyToUser(GameObject user);
        public abstract ItemStats Stats { get; }
    }
}
