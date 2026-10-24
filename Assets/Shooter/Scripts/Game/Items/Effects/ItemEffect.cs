using UnityEngine;

namespace Shooter.Game
{
    public abstract class ItemEffect : ScriptableObject
    {
        public abstract void ApplyToUser(GameObject user);
        public virtual int FoodAmount => 0;
        public virtual int HealAmount => 0;
        public virtual int IntoxicationAmount => 0;
    }
}
