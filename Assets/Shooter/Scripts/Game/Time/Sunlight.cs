using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Light))]
    public class Sunlight : MonoBehaviour
    {
        internal const float ShadowHandover = -6f;

        [SerializeField] private float brightest = 120000f;

        private Light sun;

        private void Awake()
        {
            sun = GetComponent<Light>();
        }

        private void Update()
        {
            Clock clock = GameState.Get<Clock>();
            // No clock yet on a client: the map wakes up there before the state arrives.
            // No clock any more for a frame on the way out: the world goes before its scene does
            if (clock == null) return;

            float hourAngle = (float)clock.HourAngle;
            float elevation = Celestial.Elevation(hourAngle, clock.Declination, clock.Latitude);

            transform.rotation = Celestial.Rotation(hourAngle, clock.Declination, clock.Latitude);
            sun.intensity = brightest * HorizonFade(elevation);
            sun.shadows = elevation > ShadowHandover ? LightShadows.Soft : LightShadows.None;
        }

        internal static float HorizonFade(float elevation)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-12f, 8f, elevation));
        }
    }
}
