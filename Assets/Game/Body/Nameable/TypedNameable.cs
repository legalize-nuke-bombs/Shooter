using UnityEngine;

namespace Shooter.Game.Body
{
    public sealed class TypedNameable : Nameable
    {
        [SerializeField] private NameableType type;

        public NameableType Type => type;

        public void Assign(NameableType assigned)
        {
            type = assigned;
        }

        public override string Digest()
        {
            return null;
        }
    }
}
