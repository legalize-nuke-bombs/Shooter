using System.Collections.Generic;
using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.World
{
    public class MainSpawnPoint : RegisteredBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        // A map may hold none or several: none is worth a warning, several are so many ways in
        public static MainSpawnPoint Pick()
        {
            var placed = new List<MainSpawnPoint>(Registers.Of<MainSpawnPoint>(Inactive.Exclude));

            if (placed.Count == 0)
            {
                Log.Warn("The map has no main spawn point");
                return null;
            }

            return placed[Random.Range(0, placed.Count)];
        }
    }
}
