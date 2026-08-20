using System.Linq;
using Shooter.Game.Core;
using UnityEngine;

namespace Shooter.Game.Llm
{
    public class MainDigestible : RegisteredBehaviour, IDigestible
    {
        [SerializeField] [TextArea(5, 20)] private string content;
        [SerializeField] private DigestibleSize size;

        public DigestibleSize Size => size;

        public IDigestible[] Parts { get; private set; }

        public Character Id { get; private set; }

        private void Awake()
        {
            Parts = GetComponents<IDigestible>().OrderByDescending(part => part.Priority).ToArray();
            Id = GetComponent<Character>();
        }

        public string Digest(DigestionDetail detail)
        {
            return content;
        }

        public DigestionPriority Priority => DigestionPriority.Highest;
    }
}
