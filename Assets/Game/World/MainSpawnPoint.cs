using Shooter.Game.Body;
using Shooter.Game.Llm;
using UnityEngine;

namespace Shooter.Game.World
{
    public class MainSpawnPoint : MonoBehaviour
    {
        public static MainSpawnPoint Current { get; private set; }

        private void Awake()
        {
            Current = this;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private const float Height = 2f;
        private const float Reach = 2f;
        private const float Barb = 0.3f;
        private const float Ring = 0.4f;

        private void OnDrawGizmos()
        {
            Vector3 feet = transform.position;
            Vector3 eyes = feet + Vector3.up * (Height * 0.5f + Interactor.EyeHeight);
            Vector3 ahead = eyes + transform.forward * Reach;

            Gizmos.color = new Color(0.35f, 0.9f, 1f);
            Gizmos.DrawWireSphere(feet, Ring);
            Gizmos.DrawLine(feet, feet + Vector3.up * Height);
            Gizmos.DrawLine(eyes, ahead);
            Gizmos.DrawLine(ahead, ahead - transform.forward * Barb + transform.right * Barb * 0.5f);
            Gizmos.DrawLine(ahead, ahead - transform.forward * Barb - transform.right * Barb * 0.5f);
            Gizmos.DrawLine(ahead, ahead - transform.forward * Barb + Vector3.up * Barb * 0.5f);
            Gizmos.DrawLine(ahead, ahead - transform.forward * Barb - Vector3.up * Barb * 0.5f);
        }
    }
}
