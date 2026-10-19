namespace Shooter.Infrastructure
{
    public interface ISaveable
    {
        object SaveObject();

        void LoadObject(SaveToken content);
    }
}
