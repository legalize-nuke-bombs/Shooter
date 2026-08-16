using Shooter.Game.Core;
using UnityEngine;

namespace Shooter.Game.Loot
{
    public abstract class ItemSpec : Spec
    {
        [SerializeField] private string title;
        [SerializeField] private GameObject model;
        [SerializeField] private IconSpec icon;
        [SerializeField] private Vector2Int cells = Vector2Int.one;
        [SerializeField] private string promptDescription;

        public GameObject Model => model;

        public IconSpec Icon => icon;

        public Vector2Int Cells => new Vector2Int(Mathf.Max(cells.x, 1), Mathf.Max(cells.y, 1));

        public string Title => string.IsNullOrEmpty(title) ? Key : title;

        public string PromptDescription => string.IsNullOrEmpty(promptDescription) ? Key : promptDescription;
    }
}
