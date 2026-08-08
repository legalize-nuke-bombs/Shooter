using System.Collections.Generic;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Sweeping
{
    public class Sweeper : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private float interval = 60f;

        private readonly HashSet<Sweepable> adopted = new HashSet<Sweepable>();
        private readonly List<Sweepable> ripe = new List<Sweepable>();

        private float sinceLastSweep;

        private void Awake()
        {
            enabled = false;
        }

        public void Adopt(Sweepable sweepable)
        {
            adopted.Add(sweepable);
        }

        public void Drop(Sweepable sweepable)
        {
            adopted.Remove(sweepable);
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

            foreach (Sweepable sweepable in adopted)
                if (sweepable.CanBeSwept())
                    ripe.Add(sweepable);

            foreach (Sweepable sweepable in ripe)
            {
                Log.Info("Entity {} is swept away", sweepable.name);
                sweepable.Sweep();
            }
        }
    }
}
