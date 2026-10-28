using Shooter.Infrastructure;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public class Skylight : ShooterComponent
    {
        private VolumeProfile volumeProfile;

        protected override void ShooterAwake()
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

        protected override void ShooterOnDestroy()
        {
            if (volumeProfile != null) Destroy(volumeProfile);
        }
    }
}
