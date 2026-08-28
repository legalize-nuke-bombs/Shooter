using UnityEngine;

namespace Shooter.Game.Loot
{
    public abstract class UniqueItemSpec : ItemSpec
    {
        [SerializeField] private bool equipable;

        public bool Equipable => equipable;

        public abstract UniqueItem Create();
    }
}
