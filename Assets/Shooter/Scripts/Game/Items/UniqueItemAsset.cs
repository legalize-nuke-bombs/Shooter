using UnityEngine;

namespace Shooter.Game
{
    public abstract class UniqueItemAsset : ItemAsset
    {
        [SerializeField] private bool equipable;

        public bool Equipable => equipable;

        public abstract UniqueItem Create();
    }
}
