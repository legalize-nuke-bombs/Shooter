namespace Shooter.Infrastructure
{
    public interface ISaveableComponent : ISaveableObject
    {
        void PostLoad()
        {
        }
    }
}
