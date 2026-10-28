using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public abstract class Nameable : NetworkBehaviour, IDigestible
    {
        public DigestionPriority Priority => DigestionPriority.High;

        public abstract string Digest(DigestionDetail detail);
        public abstract string GetShownName();
        public abstract string GetPromptName();
    }
}
