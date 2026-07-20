namespace Shooter.Server.Worlds.Entities.Parts
{
    public sealed class Nameable : Part
    {
        public string Name { get; }

        public Nameable(string name)
        {
            Name = name;
        }
    }
}
