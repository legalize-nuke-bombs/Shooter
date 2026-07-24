using Shooter.Server.Protocol;
using Shooter.Server.Worlds.Entities.Parts.Speaker;

namespace Shooter.Server.Worlds.Entities.Parts.Footsteps
{
    public sealed class Footsteps : Part
    {
        private const float StrideLength = 2f;

        private float strideProgress;

        public Footsteps(Entity self) : base(self, typeof(Footsteps))
        {
        }

        public override void Apply(PlayerIntent input)
        {
        }

        public override void Tick(float dt)
        {
            Movement.Movement movement = Self.Get<Movement.Movement>();
            if (movement == null) return;

            strideProgress += movement.GroundTravel;
            if (strideProgress < StrideLength) return;

            strideProgress -= StrideLength;
            Self.Get<Speaker.Speaker>()?.Play(SoundType.Footsteps);
        }

        public override void Died()
        {
        }

        public override string Digest()
        {
            return null;
        }

        public override PartState State()
        {
            return null;
        }
    }
}
