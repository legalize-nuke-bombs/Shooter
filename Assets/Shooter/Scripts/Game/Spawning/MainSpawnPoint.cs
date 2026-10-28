using Shooter.Infrastructure;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public class MainSpawnPoint : ShooterComponent
    {
        private static readonly Journal Log = Logs.Here();

        public static MainSpawnPoint PickRandom()
        {
            TArray.View<MainSpawnPoint> mainSpawnPoints = Registers.Instance.GetAll<MainSpawnPoint>(RegisterInactivePolicy.Exclude);

            if (mainSpawnPoints.Count == 0)
            {
                Log.Warn("The map has no main spawn point");
                return null;
            }

            int skipped = Random.Range(0, mainSpawnPoints.Count);
            foreach (MainSpawnPoint mainSpawnPoint in mainSpawnPoints)
            {
                if (skipped == 0) return mainSpawnPoint;
                skipped--;
            }

            return null;
        }
    }
}
