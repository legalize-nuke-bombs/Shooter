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
        [SerializeField] private int marketValue;

        public GameObject Model => model;

        public IconAsset Icon => icon;

        public Vector2Int SizeInCells => new(Mathf.Max(cells.x, 1), Mathf.Max(cells.y, 1));

        public string Title => string.IsNullOrEmpty(title) ? Id : title;

        public string Description => description;

        public virtual ItemStats Stats => new();

        public int MarketValue => marketValue;

        public virtual string PromptDescription
        {
            get
            {
                StringBuilder sb = new(promptDescription?.Trim());


                sb.Append($". Market value (per piece): {marketValue}. ");

                ItemStats stats = Stats;
                sb.Append(stats.PromptDescription());

                return sb.ToString();
            }
        }
    }
}
