using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Loading Screen Tip", fileName = "LoadingScreenTip")]
    public class LoadingScreenTipAsset : Asset
    {
        [SerializeField] [TextArea] private string text;

        public string Text => text;
    }
}
