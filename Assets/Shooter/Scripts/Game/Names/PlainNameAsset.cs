using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Plain Name", fileName = "PlainName")]
    public sealed class PlainNameAsset : NameAsset
    {
        [SerializeField] private string text;
        [SerializeField] private string prompt;

        public override string GetShownName()
        {
            return text;
        }

        public override string GetPromptName()
        {
            return prompt;
        }
    }
}
