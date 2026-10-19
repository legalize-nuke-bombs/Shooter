using System.Collections.Generic;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public class MainSpawnPoint : ShooterBehaviour
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        // A map may hold none or several: none is worth a warning, several are so many ways in
        public static MainSpawnPoint Pick()
        {
            var placed = new List<MainSpawnPoint>(Registers.GetAll<MainSpawnPoint>(RegisterInactivePolicy.Exclude));

            if (placed.Count == 0)
            {
                Log.Warn("The map has no main spawn point");
                return null;
            }

            return placed[Random.Range(0, placed.Count)];
        }
    }
}
