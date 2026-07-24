using System;
using Shooter.Server.Protocol;

namespace Shooter.Server.Worlds.Entities.Parts
{
    public abstract class Part
    {
        public Entity Self { get; }

        public Type Slot { get; }

        protected Part(Entity self, Type slot)
        {
            Self = self;
            Slot = slot;
        }

        public virtual void Apply(PlayerIntent input)
        {
        }

        public virtual void Tick(float dt)
        {
        }

        public virtual void Died()
        {
        }

        public virtual void Forget(long userId)
        {
        }

        public virtual string Digest()
        {
            return null;
        }

        public virtual PartState State()
        {
            return null;
        }
    }
}
