namespace Shooter.Server.Worlds.Entities.Parts.Nameable
{
    public sealed class KindName : Nameable
    {
        private readonly NameKind kind;

        public KindName(Entity self, NameKind kind) : base(self)
        {
            this.kind = kind;
        }

        public override string Digest()
        {
            return null;
        }

        public override PartState State()
        {
            return new KindNameState { Kind = kind };
        }
    }
}
