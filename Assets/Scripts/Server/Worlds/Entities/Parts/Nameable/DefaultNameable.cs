namespace Shooter.Server.Worlds.Entities.Parts.Nameable
{
    public sealed class DefaultNameable : Nameable
    {
        private readonly string name;

        public DefaultNameable(string name)
        {
            this.name = name;
        }

        public override string Name => name;
    }
}
