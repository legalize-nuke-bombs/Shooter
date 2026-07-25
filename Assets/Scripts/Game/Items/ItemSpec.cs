using UnityEngine;

namespace Shooter.Game.Items
{
    [CreateAssetMenu(menuName = "Shooter/Item", fileName = "Item")]
    public class ItemSpec : ScriptableObject
    {
        [SerializeField] private ItemType type;
        [SerializeField] private string title;
        [SerializeField] private int maxStack = 1;

        public ItemType Type => type;

        public string Title => string.IsNullOrEmpty(title) ? type.ToString() : title;

        public int MaxStack => Mathf.Max(maxStack, 1);

        public bool Stackable => MaxStack > 1;
    }
}
