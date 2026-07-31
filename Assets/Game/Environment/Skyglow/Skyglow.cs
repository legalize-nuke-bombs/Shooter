using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Light))]
    public class Skyglow : MonoBehaviour
    {
        [SerializeField] private float brightest = 1f;

        private Light glow;

        private void Awake()
        {
            glow = GetComponent<Light>();
            glow.shadows = LightShadows.None;
        }

        private void Update()
        {
            Environment environment = Environment.Current;
            if (environment == null) return;

            Clock clock = environment.Clock;
            float elevation = Celestial.Elevation((float)clock.HourAngle, clock.Declination, clock.Latitude);

            glow.intensity = brightest * (1f - Sunlight.HorizonFade(elevation));
        }
    }
}
