using UnityEngine;

namespace Shooter.Game
{
    public class DigestibleTransform : MonoBehaviour, IDigestible
    {
        public DigestionPriority Priority => DigestionPriority.Place;

        public string Digest(DigestionDetail detail)
        {
            return Whereabouts.Coordinates(transform.position);
        }
    }
}
