using UnityEngine;

namespace Shooter.Game
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
            moon.shadows = LightShadows.None;
        }

        private void Update()
        {
            Environment environment = Environment.Current;
            if (environment == null) return;

            Clock clock = environment.Clock;
            float hourAngle = (float)clock.HourAngle - lagBehindSun;
            float elevation = Celestial.Elevation(hourAngle, clock.Declination, clock.Latitude);

            transform.rotation = Celestial.Rotation(hourAngle, clock.Declination, clock.Latitude);
            moon.intensity = brightest * Illumination(lagBehindSun) * Sunlight.HorizonFade(elevation);
        }

        private static float Illumination(float elongation)
        {
            return (1f - Mathf.Cos(elongation * Mathf.Deg2Rad)) * 0.5f;
        }
    }
}
