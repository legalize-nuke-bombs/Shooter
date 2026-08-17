using UnityEngine;

namespace Shooter.Game.World
{
    [RequireComponent(typeof(Light))]
    public class Moonlight : MonoBehaviour
    {
        [SerializeField] private float brightest = 1.2f;

        [SerializeField] private float lagBehindSun = 144f;

        private Light moon;

        private void Awake()
        {
            moon = GetComponent<Light>();
        }

        private void Update()
        {
            Clock clock = Clock.Current;
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
