using UnityEngine;

namespace Shooter.Game.Body
{
    [CreateAssetMenu(menuName = "Shooter/Plain Name", fileName = "PlainName")]
    public sealed class PlainNameSpec : NameSpec
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
