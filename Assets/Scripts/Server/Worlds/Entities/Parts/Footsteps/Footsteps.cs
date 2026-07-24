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

        public override void Tick(float dt)
        {
            Movement.Movement motion = Self.Get<Movement.Movement>();
            if (motion == null) return;

            strideProgress += motion.GroundTravel;
            if (strideProgress < StrideLength) return;

            strideProgress -= StrideLength;
            Self.Get<Speaker.Speaker>()?.Play(SoundType.Footsteps);
        }
    }
}
