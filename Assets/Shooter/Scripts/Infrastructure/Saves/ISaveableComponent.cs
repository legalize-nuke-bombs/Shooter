namespace Shooter.Infrastructure
{
    public interface ISaveableComponent : ISaveable
    {
        string ComponentKey { get; }
    }
}
