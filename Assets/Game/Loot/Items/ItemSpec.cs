using UnityEngine;

namespace Shooter.Game.Loot
{
    [CreateAssetMenu(menuName = "Shooter/Item", fileName = "Item")]
    public class ItemSpec : Spec
    {
        [SerializeField] private string title;
        [SerializeField] private string promptName;
        [SerializeField] private bool stackable;
        [SerializeField] private bool equipable;
        [SerializeField] private GameObject model;
        [SerializeField] private Sprite icon;
        [SerializeField] private float iconScale = 1f;
        [SerializeField] private bool currency;

        public GameObject Model => model;

        public Sprite Icon => icon;

        public float IconScale => Mathf.Clamp(iconScale, 0.1f, 1f);

        public bool Currency => currency;

        public string Title => string.IsNullOrEmpty(title) ? Key : title;

        public string PromptName => string.IsNullOrEmpty(promptName) ? Key : promptName;

        public bool Stackable => stackable;

        public bool Equipable => equipable;

        public virtual UniqueItem Create(ulong id)
        {
            return new UniqueItem(id, Key);
        }
    }
}
