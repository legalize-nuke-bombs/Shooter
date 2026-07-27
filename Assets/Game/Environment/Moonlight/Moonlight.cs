using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Light))]
    public class Moonlight : MonoBehaviour
    {
        [SerializeField] private float azimuth = 170f;

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

            float overhead = (float)environment.Clock.SunOverhead - lagBehindSun;

            transform.rotation = Quaternion.Euler(overhead, azimuth, 0f);
            moon.intensity = brightest * Illumination(lagBehindSun) * Sunlight.HorizonFade(overhead);
        }

        private static float Illumination(float elongation)
        {
            return (1f - Mathf.Cos(elongation * Mathf.Deg2Rad)) * 0.5f;
        }
    }
}
