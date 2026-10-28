using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public class DigestibleTransform : ShooterComponent, IDigestible
    {
        public DigestionPriority Priority => DigestionPriority.Place;

        public string Digest(DigestionDetail detail)
        {
            return Whereabouts.FormatCoordinates(transform.position);
        }
    }
}
