using UnityEngine;

namespace Shooter.Game.Body
{
    public class MainLlmMeta : MonoBehaviour, IDigestible
    {
        [SerializeField] [TextArea(3, 10)] private string content;

        public string Digest(DigestionDetail detail)
        {
            return content;
        }

        public DigestionPriority Priority => DigestionPriority.Highest;
    }
}
