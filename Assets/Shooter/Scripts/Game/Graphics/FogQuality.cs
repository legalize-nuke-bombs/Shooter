using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Volume))]
    public class FogQuality : ShooterComponent
    {
        private static readonly Journal Log = Logs.Here();

        private ClientConfig clientConfig;
        private Fog fog;
        private string followedFog;
        private VolumeProfile profile;

        protected override void ShooterAwake()
        {
            DontDestroyOnLoad(gameObject);
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            fog = profile.Add<Fog>();
            GetComponent<Volume>().sharedProfile = profile;
        }

        protected override void ShooterOnEnable()
        {
            clientConfig = Config.GetGameConfig().Client;
            clientConfig.propertyChanged += HandleClientConfigChanged;
            FollowClientConfig();
        }

        protected override void ShooterOnDisable()
        {
            clientConfig.propertyChanged -= HandleClientConfigChanged;
        }

        protected override void ShooterOnDestroy()
        {
            Destroy(fog);
            Destroy(profile);
        }

        private void HandleClientConfigChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            FollowClientConfig();
        }

        private void FollowClientConfig()
        {
            if (clientConfig.Fog == followedFog) return;

            followedFog = clientConfig.Fog;
            switch (followedFog)
            {
                case FogQualities.Low:
                    fog.enableVolumetricFog.Override(false);
                    fog.quality.overrideState = false;
                    break;
                case FogQualities.Medium:
                    fog.enableVolumetricFog.overrideState = false;
                    fog.quality.Override((int)ScalableSettingLevelParameter.Level.Low);
                    break;
                case FogQualities.High:
                    fog.enableVolumetricFog.overrideState = false;
                    fog.quality.Override((int)ScalableSettingLevelParameter.Level.Medium);
                    break;
                case FogQualities.Max:
                    fog.enableVolumetricFog.overrideState = false;
                    fog.quality.Override((int)ScalableSettingLevelParameter.Level.High);
                    break;
            }

            Log.Info($"Fog quality is {followedFog}");
        }
    }
}
