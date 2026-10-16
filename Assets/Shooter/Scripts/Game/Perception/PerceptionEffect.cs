namespace Shooter.Game
{
    public abstract class PerceptionEffect
    {
        public abstract void Tick(float strength);

        public abstract void End();
    }
}
