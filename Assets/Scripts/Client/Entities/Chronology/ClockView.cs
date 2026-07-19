using UnityEngine;
using Shooter.Client.Worlds;
using Shooter.Server.Worlds.Entities.Chronology;
using Shooter.Logging;

namespace Shooter.Client.Entities.Chronology
{
    public class ClockView : MonoBehaviour
    {
        private const float SunYaw = 170f;
        private readonly Color dayColor = new Color(1f, 0.96f, 0.84f);
        private readonly Color nightColor = new Color(0.45f, 0.55f, 0.9f);
        private readonly float dayIntensity = 1.1f;
        private readonly float nightIntensity = 0.04f;
        private readonly float dayAmbient = 1f;
        private readonly float nightAmbient = 0.08f;

        private Light sun;

        private void Start()
        {
            sun = FindSun();
            if (sun == null)
            {
                Log.Warn("Sun not found");
                enabled = false;
            }
        }

        private void Update()
        {
            ClientWorld world = NetworkClient.Instance?.World;
            if (world?.Clock == null) return;

            float dayFraction = DayCycle.FractionOf(world.Clock.Timestamp);
            float pitch = dayFraction * 360f - 90f;
            float daylight = Mathf.Clamp01(Mathf.Sin((dayFraction - 0.25f) * Mathf.PI * 2f));

            sun.transform.rotation = Quaternion.Euler(pitch, SunYaw, 0f);
            sun.intensity = Mathf.Lerp(nightIntensity, dayIntensity, daylight);
            sun.color = Color.Lerp(nightColor, dayColor, daylight);
            RenderSettings.ambientIntensity = Mathf.Lerp(nightAmbient, dayAmbient, daylight);
        }

        private static Light FindSun()
        {
            foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
                if (light.type == LightType.Directional)
                    return light;
            return null;
        }
    }
}
