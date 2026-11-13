using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public class Sunlight : MonoBehaviour, IShooterUpdateable
    {
        public const float ShadowHandoverElevation = -4f;

        private Clock clock;
        private Light sunLight;
        private HDAdditionalLightData sunData;

        private void Awake()
        {
            sunLight = GetComponent<Light>();
            sunData = GetComponent<HDAdditionalLightData>();
        }

        private void Start()
        {
            clock = Registers.Instance.Get<Clock>();
            ShooterEntity.SetEnabled(this, clock);
        }

        public void ShooterUpdate()
        {
            float hourAngle = (float)clock.HourAngle;
            float elevation = Celestial.GetElevation(hourAngle, clock.Declination, clock.Latitude);

            transform.rotation = Celestial.GetLightRotation(hourAngle, clock.Declination, clock.Latitude);
            float fade = GetHorizonFade(elevation);
            sunData.lightDimmer = fade;
            sunData.volumetricDimmer = fade;
            sunLight.shadows = elevation > ShadowHandoverElevation ? LightShadows.Soft : LightShadows.None;
        }

        public static float GetHorizonFade(float elevation)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ShadowHandoverElevation, 0f, elevation));
        }
    }
}
