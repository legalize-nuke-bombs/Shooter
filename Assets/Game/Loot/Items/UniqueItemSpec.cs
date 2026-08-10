using UnityEngine;

namespace Shooter.Game.Loot
{
    [CreateAssetMenu(menuName = "Shooter/Unique Item", fileName = "UniqueItem")]
    public class UniqueItemSpec : ItemSpec
    {
        [SerializeField] private bool equipable;

        public bool Equipable => equipable;

        public virtual UniqueItem Create()
        {
            return new UniqueItem(Key);
        }
    }
}
