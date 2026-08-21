namespace Shooter.Game.Core.Saves
{
    public interface ISaveableComponent : ISaveable
    {
        string ComponentKey { get; }
    }
}
