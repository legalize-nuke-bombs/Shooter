using System;

namespace Shooter.Game.Loot
{
    public class Firearm : UniqueItem
    {
        public int Magazine { get; private set; }

        public Firearm(ulong id, string specId) : base(id, specId)
        {
        }

        public bool Spend()
        {
            if (Magazine == 0) return false;

            Magazine--;
            Touch();

            return true;
        }

        public int Reload(int rounds, int size)
        {
            int taken = Math.Min(rounds, size - Magazine);
            if (taken <= 0) return 0;

            Magazine += taken;
            Touch();

            return taken;
        }
    }
}
