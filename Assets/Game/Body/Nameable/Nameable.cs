using Shooter.Game.Core;
using Shooter.Game.Notifying;
using Unity.Netcode;

namespace Shooter.Game.Body
{
    public abstract class Nameable : NetworkBehaviour, IDigestible
    {
        public DigestionPriority Priority => DigestionPriority.High;

        public abstract string Digest(DigestionDetail detail);
        public abstract string PromptName();
        public abstract Arg NamedAs(string key);
    }
}
