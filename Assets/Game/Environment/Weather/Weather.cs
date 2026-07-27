using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Shooter.Game
{
    [RequireComponent(typeof(Volume))]
    public class Weather : MonoBehaviour
    {
        [SerializeField] private float dayCloudDensity = 1f;

        [SerializeField] private float nightCloudDensity = 0f;

        [SerializeField] private float transitionHours = 1.5f;

        [SerializeField] private float nightShapeFactor = 0.95f;

        private VolumetricClouds clouds;

        private float dayShapeFactor;

        private void Awake()
        {
            GetComponent<Volume>().profile.TryGet(out clouds);
            if (clouds != null) dayShapeFactor = clouds.shapeFactor.value;
        }

        private void Update()
        {
            Environment environment = Environment.Current;
            if (environment == null || clouds == null) return;

            float daylight = Daylight(environment.Clock);

            clouds.densityMultiplier.value = Mathf.Lerp(nightCloudDensity, dayCloudDensity, daylight);
            clouds.shapeFactor.value = Mathf.Lerp(nightShapeFactor, dayShapeFactor, daylight);
        }

        private float Daylight(Clock clock)
        {
            float fraction = (float)clock.DayFraction;
            float halfWindow = transitionHours / 24f / 2f;
            float dawn = Mathf.InverseLerp((float)clock.DawnFraction - halfWindow, (float)clock.DawnFraction + halfWindow, fraction);
            float dusk = 1f - Mathf.InverseLerp((float)clock.DuskFraction - halfWindow, (float)clock.DuskFraction + halfWindow, fraction);

            return Mathf.SmoothStep(0f, 1f, Mathf.Min(dawn, dusk));
        }
    }
}
