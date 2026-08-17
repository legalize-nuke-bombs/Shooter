using UnityEngine;

namespace Shooter.Game.Loot
{
    public abstract class ItemEffect : ScriptableObject
    {
        public virtual int FoodMarker => 0;
        public abstract void Apply(GameObject user);
    }
}
