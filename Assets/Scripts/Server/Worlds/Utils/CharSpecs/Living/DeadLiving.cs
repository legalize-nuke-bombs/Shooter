namespace Shooter.Server.Worlds.Utils.CharSpecs.Living
{
    public class DeadLiving : ILiving
    {
        public int Hp()
        {
            return 0;
        }

        public int MaxHp()
        {
            return 0;
        }

        public bool Alive()
        {
            return false;
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
