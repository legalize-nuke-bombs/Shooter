using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public abstract class PerceptionEffectAsset : Asset
    {
        public abstract PerceptionEffect Create();
    }
}
