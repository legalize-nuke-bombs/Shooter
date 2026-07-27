using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Light))]
    public class Moonlight : MonoBehaviour
    {
        private const float SynodicMonthDays = 29.53f;

        [SerializeField] private float azimuth = 170f;

        [SerializeField] private float brightest = 0.5f;

        [SerializeField] private float phaseAtStart = 0.5f;

        private Light moon;

        private LightShadows shadowing;

        private void Awake()
        {
            moon = GetComponent<Light>();
            shadowing = moon.shadows;
        }

        private void Update()
        {
            Environment environment = Environment.Current;
            if (environment == null) return;

            float sunOverhead = (float)environment.Clock.SunOverhead;
            float elongation = (float)(360.0 * (phaseAtStart + environment.Clock.Days / SynodicMonthDays));
            float overhead = sunOverhead - elongation;

            transform.rotation = Quaternion.Euler(overhead, azimuth, 0f);
            moon.intensity = brightest * Illumination(elongation) * Mathf.Clamp01(Mathf.Sin(overhead * Mathf.Deg2Rad));
            moon.shadows = moon.intensity > 0f && Mathf.Sin(sunOverhead * Mathf.Deg2Rad) <= 0f
                ? shadowing
                : LightShadows.None;
        }

        private static float Illumination(float elongation)
        {
            return (1f - Mathf.Cos(elongation * Mathf.Deg2Rad)) * 0.5f;
        }
    }
}
