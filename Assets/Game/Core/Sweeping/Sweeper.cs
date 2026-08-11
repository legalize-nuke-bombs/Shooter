using System.Collections.Generic;
using Shooter.Logging;
using UnityEngine;
using Environment = Shooter.Game.World.Environment;

namespace Shooter.Game.Core
{
    public class Sweeper : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private float interval = 60f;

        private readonly List<Sweepable> ripe = new List<Sweepable>();

        private float sinceLastSweep;

        private void Awake()
        {
            enabled = false;
        }

        private void Update()
        {
            sinceLastSweep += Time.deltaTime;
            if (sinceLastSweep < interval) return;

            sinceLastSweep -= interval;
            Sweep();
        }

        private void Sweep()
        {
            ripe.Clear();

            foreach (Sweepable sweepable in Environment.Current.Registers.Of<Sweepable>().All)
                if (sweepable.CanBeSwept())
                    ripe.Add(sweepable);

            foreach (Sweepable sweepable in ripe)
            {
                Log.Info($"Entity {sweepable.name} is swept away");
                sweepable.Sweep();
            }
        }
    }
}
