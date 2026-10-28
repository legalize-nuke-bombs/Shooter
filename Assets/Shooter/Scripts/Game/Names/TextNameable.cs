using Shooter.Infrastructure;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public sealed class TextNameable : Nameable
    {
        private readonly NetworkVariable<FixedString64Bytes> currentName = new();

        public string Name => currentName.Value.ToString();

        public void RenameTo(string newName)
        {
            if (!IsServer) return;

            currentName.Value = new FixedString64Bytes(newName);
        }

        public override string Digest(DigestionDetail detail)
        {
            return string.IsNullOrEmpty(Name) ? null : "Name: " + Name;
        }

        public override string GetShownName()
        {
            return Name;
        }

        public override string GetPromptName()
        {
            return Name;
        }
    }
}
