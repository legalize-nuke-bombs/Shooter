using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public class DigestibleTransform : MonoBehaviour, IDigestible
    {
        public DigestionPriority Priority => DigestionPriority.Place;

        public string Digest(DigestionDetail detail)
        {
            return Whereabouts.FormatCoordinates(transform.position);
        }
    }
}
