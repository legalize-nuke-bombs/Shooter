namespace Shooter.Game
{
    public sealed class BulletHole
    {
        public const int NoProjector = -1;

        public BulletHole(BulletHoleData data)
        {
            Data = data;
        }

        public BulletHoleData Data { get; }

        public int Projector { get; set; } = NoProjector;
    }
}
