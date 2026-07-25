using Unity.Collections;
using Unity.Netcode;

namespace Shooter.Game.Naming
{
    public sealed class GivenName : Nameable
    {
        private readonly NetworkVariable<FixedString64Bytes> given = new NetworkVariable<FixedString64Bytes>();

        public string Name => given.Value.ToString();

        public void Give(string name)
        {
            if (!IsServer) return;

            given.Value = new FixedString64Bytes(name);
        }

        public override string Digest()
        {
            return string.IsNullOrEmpty(Name) ? null : "Имя: " + Name;
        }
    }
}
