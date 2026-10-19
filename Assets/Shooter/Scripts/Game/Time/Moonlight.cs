using System.Linq;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Light))]
    public class Moonlight : MonoBehaviour
    {
        [SerializeField] private float brightest = 1.2f;

        [SerializeField] private float lagBehindSun = 144f;

        private Clock clock;
        private Light moon;

        private void Awake()
        {
            moon = GetComponent<Light>();
        }

        private void Start()
        {
            clock = Registers.GetAll<Clock>(RegisterInactivePolicy.Include).FirstOrDefault();
        }

        private void Update()
        {
            // No clock in the menu, and none any more for a frame on the way out: the world goes before its scene does
            if (clock == null) return;

            float hourAngle = (float)clock.HourAngle - lagBehindSun;
            float elevation = Celestial.Elevation(hourAngle, clock.Declination, clock.Latitude);

            transform.rotation = Celestial.Rotation(hourAngle, clock.Declination, clock.Latitude);
            moon.intensity = brightest * Illumination(lagBehindSun) * Sunlight.HorizonFade(elevation);

            float sunElevation = Celestial.Elevation((float)clock.HourAngle, clock.Declination, clock.Latitude);
            bool alone = sunElevation <= Sunlight.ShadowHandover && elevation > Sunlight.ShadowHandover;
            moon.shadows = alone ? LightShadows.Soft : LightShadows.None;
        }

        private static float Illumination(float elongation)
        {
            return (1f - Mathf.Cos(elongation * Mathf.Deg2Rad)) * 0.5f;
        }
    }
}
