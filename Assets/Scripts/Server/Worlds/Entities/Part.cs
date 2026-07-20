using System;

namespace Shooter.Server.Worlds.Entities
{
    public abstract class Part
    {
        public virtual Type Slot => GetType();

        public virtual void Tick(Entity self, float dt)
        {
        }

        public virtual PartState State()
        {
            return null;
        }
    }
}
