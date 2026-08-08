using UnityEngine;

namespace Shooter.Game.Identity
{
    public class PersistentIdProvider : MonoBehaviour
    {
        private long counter = 0;

        public long Reserve()
        {
            return counter++;
        }
    }
}
