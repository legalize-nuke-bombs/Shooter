using UnityEngine;

namespace Shooter.Game.Loot
{
    public class UniqueItemIdProvider : MonoBehaviour
    {
        private ulong last;

        public ulong Next()
        {
            return ++last;
        }
    }
}
