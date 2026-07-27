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

            float overhead = (float)environment.Clock.SunOverhead;

            transform.rotation = Quaternion.Euler(overhead, azimuth, 0f);
            sun.intensity = brightest * HorizonFade(overhead);
        }

        internal static float HorizonFade(float overhead)
        {
            float elevation = Mathf.Asin(Mathf.Sin(overhead * Mathf.Deg2Rad)) * Mathf.Rad2Deg;

            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-12f, 8f, elevation));
        }
    }
}
