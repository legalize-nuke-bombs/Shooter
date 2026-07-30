using UnityEngine;

namespace Shooter.Game.Body
{
    [CreateAssetMenu(menuName = "Shooter/Plain Name", fileName = "PlainName")]
    public sealed class PlainNameSpec : NameSpec
    {
        [SerializeField] private string text;

        public override string Text()
        {
            return text;
        }
    }
}
