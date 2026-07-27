using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Light))]
    public class Sunlight : MonoBehaviour
    {
        [SerializeField] private float azimuth = 170f;

        [SerializeField] private float brightest = 120000f;

        private Light sun;

        private LightShadows shadowing;

        private void Awake()
        {
            sun = GetComponent<Light>();
            shadowing = sun.shadows;
        }

        private void Update()
        {
            Environment environment = Environment.Current;
            if (environment == null) return;

            float overhead = (float)environment.Clock.SunOverhead;

            transform.rotation = Quaternion.Euler(overhead, azimuth, 0f);
            sun.intensity = brightest * Mathf.Clamp01(Mathf.Sin(overhead * Mathf.Deg2Rad));
            sun.shadows = sun.intensity > 0f ? shadowing : LightShadows.None;
        }
    }
}
