using System;
using Shooter.Infrastructure;
using Random = UnityEngine.Random;

namespace Shooter.Game
{
    public class MainSpawnPoint : ShooterBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        public static MainSpawnPoint PickRandom()
        {
            ReadOnlySpan<MainSpawnPoint> mainSpawnPoints = Registers.GetAll<MainSpawnPoint>(RegisterInactivePolicy.Exclude);

            if (mainSpawnPoints.Length == 0)
            {
                Log.Warn("The map has no main spawn point");
                return null;
            }

            return mainSpawnPoints[Random.Range(0, mainSpawnPoints.Length)];
        }
    }
}
