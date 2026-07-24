namespace Shooter.Server.Worlds.Entities.Parts.Nameable
{
    public class Nameable : Part
    {
        public NameableType Type { get; }
        public string Payload { get; }

        public Nameable(NameableType type, string payload)
        {
            Type = type;
            Payload = payload;
        }

        public Nameable(NameableType type)
        {
            Type = type;
        }

        public override PartState State()
        {
            return new NameableState
            {
                Type = Type,
                Payload = Payload
            };
        }
    }
}
