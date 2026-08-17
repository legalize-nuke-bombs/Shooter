using Shooter.Game.Core;
using Shooter.Game.Notifying;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body
{
    public sealed class AbsoluteNameable : Nameable
    {
        [SerializeField] private string absolute;

        private readonly NetworkVariable<FixedString64Bytes> current = new();

        public string Name => current.Value.ToString();

        public override void OnNetworkSpawn()
        {
            if (!IsServer || string.IsNullOrEmpty(absolute)) return;

            current.Value = new FixedString64Bytes(absolute);
        }

        public void Rename(string name)
        {
            if (!IsServer) return;

            current.Value = new FixedString64Bytes(name);
        }

        public override string Digest(DigestionDetail detail)
        {
            return string.IsNullOrEmpty(Name) ? null : "Name: " + Name;
        }

        public override string PromptName()
        {
            return Name;
        }

        public override Arg NamedAs(string key)
        {
            return new Arg(key, Name);
        }
    }
}
