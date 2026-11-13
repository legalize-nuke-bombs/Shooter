using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public class Moonlight : MonoBehaviour, IShooterUpdateable
    {
        private const float ShadowFadeElevations = 6f;

        [SerializeField] private float lagBehindSun = 144f;

        private Clock clock;
        private Light moonLight;
        private HDAdditionalLightData moonData;

        private void Awake()
        {
            moonLight = GetComponent<Light>();
            moonData = GetComponent<HDAdditionalLightData>();
        }

        private void Start()
        {
            clock = Registers.Instance.GetAll<Clock>(RegisterInactivePolicy.Include).TryGetFirst(out Clock first) ? first : null;
            ShooterEntity.SetEnabled(this, clock);
        }

        public void ShooterUpdate()
        {
            float hourAngle = (float)clock.HourAngle - lagBehindSun;
            float declination = clock.Declination * Mathf.Cos(lagBehindSun * Mathf.Deg2Rad);
            float elevation = Celestial.GetElevation(hourAngle, declination, clock.Latitude);

            transform.rotation = Celestial.GetLightRotation(hourAngle, declination, clock.Latitude);
            float fade = GetIlluminatedFraction(lagBehindSun) * Sunlight.GetHorizonFade(elevation);
            moonData.lightDimmer = fade;
            moonData.volumetricDimmer = fade;

            float sunElevation = Celestial.GetElevation((float)clock.HourAngle, clock.Declination, clock.Latitude);
            bool isMoonCastingShadows = sunElevation <= Sunlight.ShadowHandoverElevation && elevation > Sunlight.ShadowHandoverElevation;
            moonLight.shadows = isMoonCastingShadows ? LightShadows.Soft : LightShadows.None;
            moonData.shadowDimmer = Mathf.InverseLerp(Sunlight.ShadowHandoverElevation, Sunlight.ShadowHandoverElevation - ShadowFadeElevations, sunElevation);
        }

        private static float GetIlluminatedFraction(float elongation)
        {
            return (1f - Mathf.Cos(elongation * Mathf.Deg2Rad)) * 0.5f;
        }
    }
}
