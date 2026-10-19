using Shooter.Infrastructure;

namespace Shooter.Game
{
    public abstract class PerceptionEffectAsset : Asset
    {
        public abstract PerceptionEffect CreateEffect();
    }
}
