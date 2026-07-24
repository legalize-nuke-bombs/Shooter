namespace Shooter.Server.Worlds.Entities.Parts.Nameable
{
    public sealed class CorruptedName : Nameable
    {
        public CorruptedName(Entity self) : base(self)
        {
        }

        public override string Digest()
        {
            return null;
        }

        public override PartState State()
        {
            return new CorruptedNameState();
        }
    }
}
