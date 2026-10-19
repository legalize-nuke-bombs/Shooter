using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public sealed class AbsoluteNameable : Nameable
    {
        [SerializeField] private string absolute;

        private readonly NetworkVariable<FixedString64Bytes> currentName = new();

        public string Name => currentName.Value.ToString();

        public override void OnNetworkSpawn()
        {
            if (!IsServer || string.IsNullOrEmpty(absolute)) return;

            currentName.Value = new FixedString64Bytes(absolute);
        }

        public void RenameTo(string newName)
        {
            if (!IsServer) return;

            currentName.Value = new FixedString64Bytes(newName);
        }

        public override string Digest(DigestionDetail detail)
        {
            return string.IsNullOrEmpty(Name) ? null : "Name: " + Name;
        }

        public override string GetPromptName()
        {
            return Name;
        }
    }
}
