using System;
using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Shooter.Game.World
{
    public class Skylight : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private float angleStep = 0.25f;

        private VolumeProfile profile;
        private PhysicallyBasedSky sky;
        private double updatedAt = double.NaN;

        private void Awake()
        {
            if (GameState.Get<Clock>() == null)
            {
                Log.Info($"Entity {name} did not find clock, disabling...");
                enabled = false;
                return;
            }

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            sky = profile.Add<PhysicallyBasedSky>();
            sky.updateMode.Override(EnvironmentUpdateMode.Realtime);
            sky.updatePeriod.Override(float.MaxValue);

            var volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile;
        }

        private void OnDestroy()
        {
            if (profile != null) Destroy(profile);
        }

        private void Update()
        {
            Clock clock = GameState.Get<Clock>();
            // The world goes before its scene does: for a frame on the way out there is no clock
            if (clock == null) return;

            double hourAngle = clock.HourAngle;
            bool due = double.IsNaN(updatedAt) || Math.Abs(hourAngle - updatedAt) >= angleStep;

            // An HDRP update request reaches only the first camera of a frame, and a mirror renders before the player:
            // each camera's own realtime timer is opened for one frame instead
            sky.updatePeriod.value = due ? 0f : float.MaxValue;
            if (due) updatedAt = hourAngle;
        }
    }
}
