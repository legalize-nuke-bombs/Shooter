using UnityEngine;

namespace Shooter.Game.Loot
{
    public abstract class ItemEffect : ScriptableObject
    {
        public abstract void Apply(GameObject user);
        public virtual int FoodMarker => 0;
        public virtual int HealMarker => 0;
    }
}
