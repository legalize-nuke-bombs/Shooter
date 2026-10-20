using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Light))]
    public class Sunlight : ShooterBehaviour
    {
        internal const float ShadowHandoverElevation = -6f;

        [SerializeField] private float brightest = 120000f;

        private Clock clock;
        private Light sunLight;

        protected override void ShooterAwake()
        {
            sunLight = GetComponent<Light>();
        }

        private void Start()
        {
            clock = Registers.Get<Clock>();
        }

        private void Update()
        {
            // No clock any more for a frame on the way out: the world goes before its scene does
            if (clock == null) return;

            float hourAngle = (float)clock.HourAngle;
            float elevation = Celestial.GetElevation(hourAngle, clock.Declination, clock.Latitude);

            transform.rotation = Celestial.GetLightRotation(hourAngle, clock.Declination, clock.Latitude);
            sunLight.intensity = brightest * GetHorizonFade(elevation);
            sunLight.shadows = elevation > ShadowHandoverElevation ? LightShadows.Soft : LightShadows.None;
        }

        internal static float GetHorizonFade(float elevation)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-12f, 8f, elevation));
        }
    }
}
