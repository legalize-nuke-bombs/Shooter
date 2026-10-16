using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Shooter.Game
{
    public class Skylight : MonoBehaviour
    {
        private VolumeProfile profile;

        private void Awake()
        {
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
