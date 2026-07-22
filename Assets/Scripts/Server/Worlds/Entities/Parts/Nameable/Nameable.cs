namespace Shooter.Server.Worlds.Entities.Parts.Nameable
{
    public class Nameable : Part
    {
        private readonly NameableType type;
        private readonly string payload;

        public Nameable(NameableType type, string payload)
        {
            this.type = type;
            this.payload = payload;
        }

        public Nameable(NameableType type)
        {
            this.type = type;
        }

        public override PartState State()
        {
            return new NameableState
            {
                Type = type,
                Payload = payload
            };
        }
    }
}
