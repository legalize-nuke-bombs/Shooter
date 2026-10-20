using Shooter.Infrastructure;
using Unity.Netcode;

namespace Shooter.Game
{
    public abstract class Nameable : ShooterNetworkBehaviour, IDigestible
    {
        public DigestionPriority Priority => DigestionPriority.High;

        public abstract string Digest(DigestionDetail detail);
        public abstract string GetShownName();
        public abstract string GetPromptName();
    }
}
