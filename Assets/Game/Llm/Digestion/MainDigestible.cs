using System.Linq;
using Shooter.Game.Core;
using UnityEngine;
using Environment = Shooter.Game.World.Environment;

namespace Shooter.Game.Llm
{
    public class MainDigestible : MonoBehaviour, IDigestible
    {
        [SerializeField] [TextArea(5, 20)] private string content;
        [SerializeField] private DigestibleSize size;

        private IDigestible[] parts;

        private PersistentId id;

        private long registered;

        public DigestibleSize Size => size;

        public IDigestible[] Parts => parts;

        public PersistentId Id => id;

        private void Awake()
        {
            parts = this.FindAll<IDigestible>().OrderByDescending(part => part.Priority).ToArray();
            id = GetComponent<PersistentId>();
        }

        private void OnEnable()
        {
            registered = Registers.Current.Of<MainDigestible>().Add(this);
        }

        private void OnDisable()
        {
            Registers world = Registers.Current;
            if (world != null) world.Of<MainDigestible>().Remove(registered);
        }

        public string Digest(DigestionDetail detail)
        {
            return content;
        }

        public DigestionPriority Priority => DigestionPriority.Highest;
    }
}
