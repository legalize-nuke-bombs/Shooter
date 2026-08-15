using UnityEngine;

namespace Shooter.Game.Loot
{
    public abstract class ItemEffect : ScriptableObject
    {
        public abstract void Apply(GameObject user);
    }
}
