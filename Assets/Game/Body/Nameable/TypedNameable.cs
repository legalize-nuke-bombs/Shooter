using UnityEngine;

namespace Shooter.Game.Body
{
    public sealed class TypedNameable : Nameable
    {
        [SerializeField] private NameableType type;

        public NameableType Type => type;

        public override string Digest()
        {
            return null;
        }
    }
}
