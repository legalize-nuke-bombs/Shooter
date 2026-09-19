using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.World
{
    [RequireComponent(typeof(Light))]
    public class Sunlight : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        internal const float ShadowHandover = -6f;

        [SerializeField] private float brightest = 120000f;

        private Light sun;

        private void Awake()
        {
            sun = GetComponent<Light>();
            if (GameState.Get<Clock>() == null)
            {
                Log.Info($"Entity {name} did not find clock, disabling...");
                enabled = false;
            }
        }

        private void Update()
        {
            Clock clock = GameState.Get<Clock>();
            // The world goes before its scene does: for a frame on the way out there is no clock
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
