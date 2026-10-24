using System.Text;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public abstract class ItemAsset : Asset
    {
        [SerializeField] private string title;
        [SerializeField] [TextArea(2, 6)] private string description;
        [SerializeField] private GameObject model;
        [SerializeField] private IconAsset icon;
        [SerializeField] private Vector2Int cells = Vector2Int.one;
        [SerializeField] private string promptDescription;

        public GameObject Model => model;

        public IconAsset Icon => icon;

        public Vector2Int SizeInCells => new(Mathf.Max(cells.x, 1), Mathf.Max(cells.y, 1));

        public string Title => string.IsNullOrEmpty(title) ? Id : title;

        public string Description => description;

        public virtual ItemStats Stats => new();

        public virtual string PromptDescription
        {
            get
            {
                StringBuilder description = new(promptDescription?.Trim());
                ItemStats stats = Stats;

                description.Append('.');
                if (stats.Heal > 0) description.Append($" Heal {stats.Heal}.");
                if (stats.HealPerMinute > 0) description.Append($" Heal {stats.HealPerMinute} per minute.");
                if (stats.Food > 0) description.Append($" Food {stats.Food}.");
                if (stats.Intoxication > 0) description.Append($" Intoxication {stats.Intoxication}.");
                if (stats.Damage > 0) description.Append($" Damage {stats.Damage}.");
                if (stats.Magazine > 0) description.Append($" Magazine {stats.Magazine}.");

                return description.ToString();
            }
        }
    }
}
