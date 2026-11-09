using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Game
{
    public readonly struct ShotHit
    {
        public ShotHit(Ray ray, IReadOnlyList<RaycastHit> hits, RaycastHit nearest)
        {
            Ray = ray;
            Hits = hits;
            Nearest = nearest;
        }

        public Ray Ray { get; }

        public IReadOnlyList<RaycastHit> Hits { get; }

        public RaycastHit Nearest { get; }

        public bool IsMiss => Hits.Count == 0;
    }
}
