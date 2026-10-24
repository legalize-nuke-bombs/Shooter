using System.Collections.Generic;
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

        // Its numbers, each kind of thing knows its own: the tooltip shows them, the prompt description tells them
        public virtual IEnumerable<ItemStat> Stats
        {
            get { yield break; }
        }

        public virtual string PromptDescription
        {
            get
            {
                StringBuilder description = new((promptDescription ?? string.Empty).Trim());

                description.Append('.');
                foreach (ItemStat stat in Stats)
                {
                    description.Append(' ').Append(stat.PromptDescription).Append('.');
                }

                return description.ToString();
            }
        }
    }
}
