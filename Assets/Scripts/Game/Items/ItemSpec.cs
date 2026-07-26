using UnityEngine;

namespace Shooter.Game.Items
{
    [CreateAssetMenu(menuName = "Shooter/Item", fileName = "Item")]
    public class ItemSpec : ScriptableObject
    {
        [SerializeField] private ItemType type;
        [SerializeField] private string promptName;
        [SerializeField] private bool stackable;
        [SerializeField] private bool equipable;

        public ItemType Type => type;

        public string PromptName => string.IsNullOrEmpty(promptName) ? type.ToString() : promptName;

        public bool Stackable => stackable;

        public bool Equipable => equipable;
    }
}
