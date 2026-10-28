using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine;

namespace Shooter.Game
{
    public class Skylight : MonoBehaviour
    {
        private VolumeProfile volumeProfile;

        private void Awake()
        {
            volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            PhysicallyBasedSky physicallyBasedSky = volumeProfile.Add<PhysicallyBasedSky>();
            physicallyBasedSky.updateMode.Override(EnvironmentUpdateMode.Realtime);
            physicallyBasedSky.updatePeriod.Override(0f);

            Volume volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = volumeProfile;
        }

        private void OnDestroy()
        {
            if (volumeProfile != null) Destroy(volumeProfile);
        }
    }
}
