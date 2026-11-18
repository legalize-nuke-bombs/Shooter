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
            if (RenderPipelineManager.currentPipeline is { disposed: false }) SetProbeVolumesInitialized(true);
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
            SetProbeVolumesInitialized(probeVolumes);

            foreach (Camera view in cameras)
            {
                if (view.cameraType != CameraType.Game || !view.TryGetComponent(out HDAdditionalCameraData cameraData)) continue;

                cameraData.customRenderingSettings = true;
                cameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.AdaptiveProbeVolume] = true;
                cameraData.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.AdaptiveProbeVolume, probeVolumes);
            }
        }

        // The pool and the index stay in video memory while the system is initialized, whatever the cameras render;
        // HDRP initializes it again whenever it recreates the pipeline, so this runs every frame
        private static void SetProbeVolumesInitialized(bool initialized)
        {
            if (GraphicsSettings.currentRenderPipeline is not HDRenderPipelineAsset asset) return;

            RenderPipelineSettings settings = asset.currentPlatformRenderPipelineSettings;
            if (settings.lightProbeSystem != RenderPipelineSettings.LightProbeSystem.AdaptiveProbeVolumes) return;

            ProbeReferenceVolume system = ProbeReferenceVolume.instance;
            if (system.isInitialized == initialized) return;

            if (initialized)
            {
                system.Initialize(new ProbeVolumeSystemParameters
                {
                    memoryBudget = settings.probeVolumeMemoryBudget,
                    blendingMemoryBudget = settings.probeVolumeBlendingMemoryBudget,
                    shBands = settings.probeVolumeSHBands,
                    supportScenarios = settings.supportProbeVolumeScenarios,
                    supportScenarioBlending = settings.supportProbeVolumeScenarioBlending,
                    supportDiskStreaming = settings.supportProbeVolumeDiskStreaming,
                    supportGPUStreaming = settings.supportProbeVolumeGPUStreaming
                });
            }
            else
            {
                system.Cleanup();
            }

            Log.Info($"Adaptive probe volumes {(initialized ? "initialized" : "cleaned up")}");
        }
    }
}
