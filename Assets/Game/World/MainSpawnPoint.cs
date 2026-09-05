using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.World
{
    public class MainSpawnPoint : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        public static MainSpawnPoint Current { get; private set; }

        private void Awake()
        {
            if (Current != null)
            {
                Log.Error("Singleton class has more than one instance");
            }
            Current = this;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }
    }
}
