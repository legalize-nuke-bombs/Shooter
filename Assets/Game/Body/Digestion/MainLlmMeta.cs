using UnityEngine;

namespace Shooter.Game.Body
{
    public class MainLlmMeta : MonoBehaviour, IDigestible
    {
        [SerializeField] private string content;

        public string Digest(DigestionDetail detail)
        {
            return content;
        }

        public DigestionPriority Priority => DigestionPriority.Highest;
    }
}
