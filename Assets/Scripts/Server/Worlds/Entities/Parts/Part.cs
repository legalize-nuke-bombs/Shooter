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

        public abstract void Apply(PlayerIntent input);

        public abstract void Tick(float dt);

        public abstract void Died();

        public abstract string Digest();

        public abstract PartState State();
    }
}
