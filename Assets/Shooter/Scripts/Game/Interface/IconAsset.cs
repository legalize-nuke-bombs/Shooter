using UnityEngine;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Icon", fileName = "Icon")]
    public class IconAsset : Asset
    {
        [SerializeField] private Sprite sprite;
        [SerializeField] private string promptDescription;

        public Sprite Sprite => sprite;
        public string PromptDescription => promptDescription;
    }
}
