using UnityEngine;

namespace Shooter.Game.Lighting
{
    [RequireComponent(typeof(Light))]
    public class Sunlight : MonoBehaviour
    {
        private const float DayLengthSeconds = 86400f;
        private const float DawnAngle = -90f;

        [SerializeField] private float azimuth = 170f;

        [SerializeField] private float brightest = 1.4f;

        private Light sun;

        private void Awake()
        {
            sun = GetComponent<Light>();
        }

        private void Update()
        {
            Environment environment = Environment.Current;
            if (environment == null) return;

            float overhead = Overhead(environment);

            transform.rotation = Quaternion.Euler(overhead, azimuth, 0f);
            sun.intensity = brightest * Mathf.Clamp01(Mathf.Sin(overhead * Mathf.Deg2Rad));
        }

        private static float Overhead(Environment environment)
        {
            float passed = (float)(environment.Clock.Now.TimeOfDay.TotalSeconds / DayLengthSeconds);

            return passed * 360f + DawnAngle;
        }
    }
}
