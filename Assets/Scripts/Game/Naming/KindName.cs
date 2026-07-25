using UnityEngine;

namespace Shooter.Game.Naming
{
    public sealed class KindName : Nameable
    {
        [SerializeField] private NameKind kind;

        public NameKind Kind => kind;

        public override string Digest()
        {
            return null;
        }
    }
}
