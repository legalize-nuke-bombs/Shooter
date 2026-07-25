using UnityEngine;

namespace Shooter.Game.Items
{
    [CreateAssetMenu(menuName = "Shooter/Item", fileName = "Item")]
    public class ItemSpec : ScriptableObject
    {
        [SerializeField] private ItemType type;
        [SerializeField] private string title;
        [SerializeField] private bool stackable;

        public ItemType Type => type;

        public string Title => string.IsNullOrEmpty(title) ? type.ToString() : title;

        public bool Stackable => stackable;
    }
}
