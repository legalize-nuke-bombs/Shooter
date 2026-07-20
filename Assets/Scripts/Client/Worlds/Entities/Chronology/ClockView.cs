using UnityEngine;
using Shooter.Client.Worlds;
using Shooter.Server.Worlds.Time;
using Shooter.Logging;

namespace Shooter.Client.Worlds.Entities.Chronology
{
    public class ClockView
    {
        private const float SunYaw = 170f;
        private const float DayIntensity = 1.1f;
        private const float NightIntensity = 0.04f;
        private const float DayAmbient = 1f;
        private const float NightAmbient = 0.08f;

        private static readonly Color DayColor = new Color(1f, 0.96f, 0.84f);
        private static readonly Color NightColor = new Color(0.45f, 0.55f, 0.9f);

        private readonly ClientWorld world;
        private readonly Light sun;

        public ClockView(ClientWorld world)
        {
            this.world = world;
            sun = FindSun();

            if (sun == null)
                Log.Warn("Sky: no directional light in scene, day cycle will not render");
        }

        public void Tick()
        {
            if (sun == null || world.Clock == null) return;

            float dayFraction = DayCycle.FractionOf(world.Clock.Timestamp);
            float pitch = dayFraction * 360f - 90f;
            float daylight = Mathf.Clamp01(Mathf.Sin((dayFraction - 0.25f) * Mathf.PI * 2f));

            sun.transform.rotation = Quaternion.Euler(pitch, SunYaw, 0f);
            sun.intensity = Mathf.Lerp(NightIntensity, DayIntensity, daylight);
            sun.color = Color.Lerp(NightColor, DayColor, daylight);
            RenderSettings.ambientIntensity = Mathf.Lerp(NightAmbient, DayAmbient, daylight);
        }

        private static Light FindSun()
        {
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
                if (light.type == LightType.Directional)
                    return light;
            return null;
        }
    }
}
