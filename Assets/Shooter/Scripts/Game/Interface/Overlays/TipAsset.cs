using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Tip", fileName = "Tip")]
    public class TipAsset : Asset
    {
        [SerializeField] [TextArea] private string text;

        public string Text => text;
    }
}
