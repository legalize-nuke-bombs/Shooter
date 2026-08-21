namespace Shooter.Game.Core.Saves
{
    public interface ISaveable
    {
        object SaveObject();

        void LoadObject(SaveToken content);
    }
}
