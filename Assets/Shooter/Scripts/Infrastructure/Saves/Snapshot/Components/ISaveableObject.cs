namespace Shooter.Infrastructure
{
    public interface ISaveableObject
    {
        object SaveObject();

        void LoadObject(SaveToken content);
    }
}
