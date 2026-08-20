using System.Linq;
using Shooter.Game.Core;
using UnityEngine;

namespace Shooter.Game.Llm
{
    public class MainDigestible : MonoBehaviour, IDigestible
    {
        [SerializeField] [TextArea(5, 20)] private string content;
        [SerializeField] private DigestibleSize size;

        private long registered;

        public DigestibleSize Size => size;

        public IDigestible[] Parts { get; private set; }

        public Character Id { get; private set; }

        private void Awake()
        {
            Parts = GetComponents<IDigestible>().OrderByDescending(part => part.Priority).ToArray();
            Id = GetComponent<Character>();
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
