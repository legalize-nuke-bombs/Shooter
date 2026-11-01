using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public class Sunlight : MonoBehaviour, IShooterUpdateable
    {
        public const float ShadowHandoverElevation = -6f;

        [SerializeField] private float brightest = 120000f;

        private Clock clock;
        private Light sunLight;

        private void Awake()
        {
            sunLight = GetComponent<Light>();
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
            sunLight.intensity = brightest * GetHorizonFade(elevation);
            sunLight.shadows = elevation > ShadowHandoverElevation ? LightShadows.Soft : LightShadows.None;
        }

        public static float GetHorizonFade(float elevation)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-12f, 8f, elevation));
        }
    }
}
