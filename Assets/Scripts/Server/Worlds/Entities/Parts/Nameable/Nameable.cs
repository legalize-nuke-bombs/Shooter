using Shooter.Server.Protocol;

namespace Shooter.Server.Worlds.Entities.Parts.Nameable
{
    public abstract class Nameable : Part
    {
        protected Nameable(Entity self) : base(self, typeof(Nameable))
        {
        }

        public sealed override void Apply(PlayerIntent input)
        {
        }

        public sealed override void Tick(float dt)
        {
        }

        public sealed override void Died()
        {
        }
    }
}
