namespace Shooter.Server.Worlds.Entities.CharSpecs.Living
{
    public class ImmortalLiving : ILiving
    {
        public int Hp()
        {
            return 1;
        }

        public int MaxHp()
        {
            return 1;
        }

        public bool Alive()
        {
            return true;
        }

        public void Damage(int amount)
        {

        }

        public void Heal(int amount)
        {

        }

        public void Kill()
        {

        }

        public void Resurrect()
        {

        }
    }
}
