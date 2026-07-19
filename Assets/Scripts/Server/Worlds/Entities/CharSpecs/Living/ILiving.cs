namespace Shooter.Server.Worlds.Entities.CharSpecs.Living
{
    public interface ILiving
    {
        int Hp();

        int MaxHp();

        bool Alive();

        void Damage(int amount);

        void Heal(int amount);

        void Kill();

        void Resurrect();
    }
}
