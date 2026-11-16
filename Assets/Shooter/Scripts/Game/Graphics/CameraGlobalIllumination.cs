using System.Collections.Generic;
using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public class CameraGlobalIllumination : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private ClientConfig clientConfig;
        private string followedGlobalIllumination;
        private bool probeVolumes;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            clientConfig = Config.GetGameConfig().Client;
            clientConfig.propertyChanged += HandleClientConfigChanged;
            RenderPipelineManager.beginContextRendering += ApplyToGameCameras;
            FollowClientConfig();
        }

        private void OnDisable()
        {
            clientConfig.propertyChanged -= HandleClientConfigChanged;
            RenderPipelineManager.beginContextRendering -= ApplyToGameCameras;
        }

        private void HandleClientConfigChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            FollowClientConfig();
        }

        private void FollowClientConfig()
        {
            if (clientConfig.GlobalIllumination == followedGlobalIllumination) return;

            followedGlobalIllumination = clientConfig.GlobalIllumination;
            probeVolumes = followedGlobalIllumination == Toggles.On;

            Log.Info($"Game cameras render with global illumination {followedGlobalIllumination}");
        }

        private void ApplyToGameCameras(ScriptableRenderContext context, List<Camera> cameras)
        {
            foreach (Camera view in cameras)
            {
                if (view.cameraType != CameraType.Game || !view.TryGetComponent(out HDAdditionalCameraData cameraData)) continue;

                cameraData.customRenderingSettings = true;
                cameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.AdaptiveProbeVolume] = true;
                cameraData.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.AdaptiveProbeVolume, probeVolumes);
            }
        }
    }
}
