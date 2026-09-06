using Shooter.Game.Core;
using Shooter.Game.World;
using UnityEngine;

namespace Shooter.Game.Llm
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
