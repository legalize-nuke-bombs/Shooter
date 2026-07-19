namespace Shooter.Server.Worlds.Utils.CharSpecs.Nameable
{
    public class DefaultNameable : INameable
    {
        private readonly string name;

        public DefaultNameable(string name)
        {
            this.name = name;
        }

        public string Name()
        {
            return name;
        }
    }
}
