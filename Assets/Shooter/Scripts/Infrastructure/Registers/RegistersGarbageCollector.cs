using UnityEngine;

namespace Shooter.Infrastructure
{
    public class RegistersGarbageCollector : ShooterBehaviour
    {
        [SerializeField] private float collectInterval = 2f;

        private float nextCollectTime;

        protected override void ShooterAwake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime < nextCollectTime) return;

            nextCollectTime = Time.unscaledTime + collectInterval;
            Registers.CollectGarbage();
        }
    }
}
