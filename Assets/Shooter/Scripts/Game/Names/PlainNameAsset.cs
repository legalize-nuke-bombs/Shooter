using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Plain Name", fileName = "PlainName")]
    public sealed class PlainNameAsset : NameAsset
    {
        [SerializeField] private string text;
        [SerializeField] private string prompt;

        public override string Text()
        {
            return text;
        }

        public override string Prompt()
        {
            return prompt;
        }
    }
}
