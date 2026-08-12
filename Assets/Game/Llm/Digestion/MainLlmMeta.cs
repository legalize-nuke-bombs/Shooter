using UnityEngine;

namespace Shooter.Game.Llm
{
    public class MainLlmMeta : MonoBehaviour, IDigestible
    {
        [SerializeField] [TextArea(5, 20)] private string content;
        [SerializeField] private DigestibleSize size;

        public string Digest(DigestionDetail detail)
        {
            return content;
        }

        public DigestionPriority Priority => DigestionPriority.Highest;

        public DigestibleSize? Size => size;
    }
}
