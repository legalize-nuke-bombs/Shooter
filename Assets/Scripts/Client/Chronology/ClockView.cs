using UnityEngine;
using Shooter.Server.Worlds;
using Shooter.Logging;

namespace Shooter.Client.Chronology
{
    public class ClockView : MonoBehaviour
    {
        private const float SunYaw = 170f;
        private readonly Color dayColor = new Color(1f, 0.96f, 0.84f);
        private readonly Color nightColor = new Color(0.45f, 0.55f, 0.9f);
        private float dayIntensity = 1.1f;
        private float nightIntensity = 0.04f;
        private float dayAmbient = 1f;
        private float nightAmbient = 0.08f;

        private Light sun;

        private void Start()
        {
            sun = FindSun();
            if (sun == null)
            {
                Log.Warn("no directional light in scene, sky won't move");
                enabled = false;
                return;
            }
            NetworkClient.Instance.SnapshotReceived += OnSnapshot;
        }

        private void OnDestroy()
        {
            if (NetworkClient.Instance == null) return;
            NetworkClient.Instance.SnapshotReceived -= OnSnapshot;
        }

        private void OnSnapshot(Snapshot snapshot)
        {
            Apply(snapshot.clock.Fraction());
        }

        private void Apply(float dayFraction)
        {
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
