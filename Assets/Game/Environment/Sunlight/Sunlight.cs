using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Light))]
    public class Sunlight : MonoBehaviour
    {
        [SerializeField] private float brightest = 120000f;

        private Light sun;

        private void Awake()
        {
            sun = GetComponent<Light>();
        }

        private void Update()
        {
            Environment environment = Environment.Current;
            if (environment == null) return;

            Clock clock = environment.Clock;
            float hourAngle = (float)clock.HourAngle;
            float elevation = Celestial.Elevation(hourAngle, clock.Declination, clock.Latitude);

            transform.rotation = Celestial.Rotation(hourAngle, clock.Declination, clock.Latitude);
            sun.intensity = brightest * HorizonFade(elevation);
        }

        internal static float HorizonFade(float elevation)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-12f, 8f, elevation));
        }
    }
}
