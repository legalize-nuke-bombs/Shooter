using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Light))]
    public class Sunlight : MonoBehaviour
    {
        [SerializeField] private float azimuth = 170f;

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

            float overhead = Overhead(environment);

            transform.rotation = Quaternion.Euler(overhead, azimuth, 0f);
            sun.intensity = brightest * Mathf.Clamp01(Mathf.Sin(overhead * Mathf.Deg2Rad));
        }

        private static float Overhead(Environment environment)
        {
            return (float)((environment.Clock.DayFraction - Clock.DawnFraction) * 360.0);
        }
    }
}
