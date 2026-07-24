using Shooter.Server.Protocol;

namespace Shooter.Server.Worlds.Entities.Parts.Nameable
{
    public sealed class Nameable : Part
    {
        public NameableType Type { get; }
        public string Payload { get; }

        public Nameable(Entity self, NameableType type, string payload) : base(self, typeof(Nameable))
        {
            Type = type;
            Payload = payload;
        }

        public Nameable(Entity self, NameableType type) : this(self, type, null)
        {
        }

        public override void Apply(PlayerIntent input)
        {
        }

        public override void Tick(float dt)
        {
        }

        public override void Died()
        {
        }

        public override string Digest()
        {
            return string.IsNullOrEmpty(Payload) ? null : "Имя: " + Payload;
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
