using UnityEngine;

namespace Shooter.Game
{
    public abstract class UniqueItemAsset : ItemAsset
    {
        [SerializeField] private bool equipable;

        public bool IsEquipable => equipable;

        public abstract UniqueItem CreateItem();
    }
}
