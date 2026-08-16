namespace Shooter.Game.Body.Perception
{
    public abstract class PerceptionEffect
    {
        public abstract void Tick(float strength);

        public abstract void End();
    }
}
