using System.Collections.Generic;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public class MainSpawnPoint : ShooterBehaviour
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        public static MainSpawnPoint PickRandom()
        {
            var mainSpawnPoints = new List<MainSpawnPoint>(Registers.GetAll<MainSpawnPoint>(RegisterInactivePolicy.Exclude));

            if (mainSpawnPoints.Count == 0)
            {
                Log.Warn("The map has no main spawn point");
                return null;
            }

            return mainSpawnPoints[Random.Range(0, mainSpawnPoints.Count)];
        }
    }
}
