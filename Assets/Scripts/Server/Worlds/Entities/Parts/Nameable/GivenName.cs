namespace Shooter.Server.Worlds.Entities.Parts.Nameable
{
    public sealed class GivenName : Nameable
    {
        private readonly string name;

        public GivenName(Entity self, string name) : base(self)
        {
            this.name = name;
        }

        public override string Digest()
        {
            return string.IsNullOrEmpty(name) ? null : "Имя: " + name;
        }

        public override PartState State()
        {
            return new GivenNameState { Name = name };
        }
    }
}
