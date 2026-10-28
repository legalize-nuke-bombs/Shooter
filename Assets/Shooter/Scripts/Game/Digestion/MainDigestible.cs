using System.Linq;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(DigestibleTransform))]
    public class MainDigestible : MonoBehaviour, IDigestible
    {
        public const string ContentField = nameof(content);
        public const string SizeField = nameof(size);

        [SerializeField] [TextArea(5, 20)] private string content;
        [SerializeField] private DigestibleSize size;

        public DigestibleSize Size => size;

        public IDigestible[] Parts { get; private set; }

        private void Awake()
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
