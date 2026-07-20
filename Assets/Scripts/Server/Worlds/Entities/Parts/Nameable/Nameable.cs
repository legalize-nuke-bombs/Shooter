using System;

namespace Shooter.Server.Worlds.Entities.Parts.Nameable
{
    public abstract class Nameable : Part
    {
        public sealed override Type Slot => typeof(Nameable);

        public abstract string Name { get; }

        public override PartState State()
        {
            return new NameState { Name = Name };
        }
    }
}
