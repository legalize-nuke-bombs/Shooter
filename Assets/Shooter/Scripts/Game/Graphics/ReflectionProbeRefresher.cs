using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HDAdditionalReflectionData))]
    public class ReflectionProbeRefresher : MonoBehaviour, IShooterUpdateable
    {
        [SerializeField] private float refreshSeconds = 1f;

        private HDAdditionalReflectionData probe;
        private float secondsSinceRefresh;

        private void Awake()
        {
            probe = GetComponent<HDAdditionalReflectionData>();
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void ShooterUpdate()
        {
            secondsSinceRefresh += Time.deltaTime;
            if (secondsSinceRefresh >= refreshSeconds) Refresh();
        }

        private void Refresh()
        {
            secondsSinceRefresh = 0f;
            probe.RequestRenderNextUpdate();
        }
    }
}
