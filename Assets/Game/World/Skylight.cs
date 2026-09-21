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

        private VolumeProfile profile;

        private void Awake()
        {
            if (GameState.Get<Clock>() == null)
            {
                Log.Info($"Entity {name} did not find clock, disabling...");
                enabled = false;
                return;
            }

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var sky = profile.Add<PhysicallyBasedSky>();
            sky.updateMode.Override(EnvironmentUpdateMode.Realtime);
            sky.updatePeriod.Override(0f);

            var volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile;
        }

        private void OnDestroy()
        {
            if (profile != null) Destroy(profile);
        }
    }
}
