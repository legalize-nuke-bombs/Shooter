using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public class DigestibleTransform : ShooterBehaviour, IDigestible
    {
        public DigestionPriority Priority => DigestionPriority.Place;

        public string Digest(DigestionDetail detail)
        {
            return Whereabouts.FormatCoordinates(transform.position);
        }
    }
}
