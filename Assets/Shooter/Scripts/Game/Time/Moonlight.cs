using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public class Moonlight : ShooterComponent, IShooterUpdateable
    {
        [SerializeField] private float brightest = 1.2f;

        [SerializeField] private float lagBehindSun = 144f;

        private Clock clock;
        private Light moonLight;

        protected override void ShooterAwake()
        {
            moonLight = GetComponent<Light>();
        }

        private void Start()
        {
            clock = Registers.Instance.GetAll<Clock>(RegisterInactivePolicy.Include).TryGetFirst(out Clock first) ? first : null;
        }

        public void ShooterUpdate()
        {
            // No clock in the menu, and none any more for a frame on the way out: the world goes before its scene does
            if (clock == null) return;

            float hourAngle = (float)clock.HourAngle - lagBehindSun;
            float elevation = Celestial.GetElevation(hourAngle, clock.Declination, clock.Latitude);

            transform.rotation = Celestial.GetLightRotation(hourAngle, clock.Declination, clock.Latitude);
            moonLight.intensity = brightest * GetIlluminatedFraction(lagBehindSun) * Sunlight.GetHorizonFade(elevation);

            float sunElevation = Celestial.GetElevation((float)clock.HourAngle, clock.Declination, clock.Latitude);
            bool isMoonCastingShadows = sunElevation <= Sunlight.ShadowHandoverElevation && elevation > Sunlight.ShadowHandoverElevation;
            moonLight.shadows = isMoonCastingShadows ? LightShadows.Soft : LightShadows.None;
        }

        private static float GetIlluminatedFraction(float elongation)
        {
            return (1f - Mathf.Cos(elongation * Mathf.Deg2Rad)) * 0.5f;
        }
    }
}
