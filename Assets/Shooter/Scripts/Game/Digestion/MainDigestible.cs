using System.Linq;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(DigestibleTransform))]
    public class MainDigestible : ShooterBehaviour, IDigestible
    {
        [SerializeField] [TextArea(5, 20)] private string content;
        [SerializeField] private DigestibleSize size;

        public DigestibleSize Size => size;

        public IDigestible[] Parts { get; private set; }

        protected override void Awakened()
        {
            Parts = GetComponents<IDigestible>().OrderByDescending(part => part.Priority).ToArray();
        }

        public string Digest(DigestionDetail detail)
        {
            return content;
        }

        public DigestionPriority Priority => DigestionPriority.Head;
    }
}
