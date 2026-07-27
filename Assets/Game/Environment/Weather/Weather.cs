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

            float daylight = Daylight((float)environment.Clock.DayFraction);

            clouds.densityMultiplier.value = Mathf.Lerp(nightCloudDensity, dayCloudDensity, daylight);
            clouds.shapeFactor.value = Mathf.Lerp(nightShapeFactor, dayShapeFactor, daylight);
        }

        private float Daylight(float fraction)
        {
            float halfWindow = transitionHours / 24f / 2f;
            float dawn = Mathf.InverseLerp((float)Clock.DawnFraction - halfWindow, (float)Clock.DawnFraction + halfWindow, fraction);
            float dusk = 1f - Mathf.InverseLerp((float)Clock.DuskFraction - halfWindow, (float)Clock.DuskFraction + halfWindow, fraction);

            return Mathf.SmoothStep(0f, 1f, Mathf.Min(dawn, dusk));
        }
    }
}
