using System.Collections.Generic;
using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public class CameraUpscaling : ShooterComponent
    {
        // DLSS quality codes as the NVIDIA module numbers them; the camera keeps them as a plain number
        private const uint DlssPerformance = 0;
        private const uint DlssBalanced = 1;
        private const uint DlssQuality = 2;
        private const uint DlssUltraPerformance = 3;
        private const uint Dlaa = 4;
        private const float FullRenderPercentage = 100f;
        private static readonly Journal Log = Logs.Here();

        private ClientConfig clientConfig;
        private string followedUpscaler;
        private string followedRenderScale;
        private string followedAntialiasing;
        private bool upscaling;
        private bool dlss;
        private uint dlssQuality = Dlaa;
        private float renderPercentage = FullRenderPercentage;
        private HDAdditionalCameraData.AntialiasingMode antialiasing;

        protected override void ShooterAwake()
        {
            DontDestroyOnLoad(gameObject);
            DynamicResolutionHandler.SetDynamicResScaler(() => renderPercentage, DynamicResScalePolicyType.ReturnsPercentage);
        }

        protected override void ShooterOnEnable()
        {
            clientConfig = Config.GetGameConfig().Client;
            clientConfig.propertyChanged += HandleClientConfigChanged;
            RenderPipelineManager.beginContextRendering += ApplyToGameCameras;
            FollowClientConfig();
        }

        protected override void ShooterOnDisable()
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
            if (clientConfig.Upscaler == followedUpscaler && clientConfig.RenderScale == followedRenderScale && clientConfig.Antialiasing == followedAntialiasing) return;

            followedUpscaler = clientConfig.Upscaler;
            followedRenderScale = clientConfig.RenderScale;
            followedAntialiasing = clientConfig.Antialiasing;
            FollowUpscaler(followedUpscaler);
            FollowRenderScale(followedRenderScale);
            FollowAntialiasing(followedAntialiasing);

            Log.Info($"Game cameras render with upscaler {followedUpscaler} at {renderPercentage}% and antialiasing {followedAntialiasing}");
        }

        private void FollowUpscaler(string upscaler)
        {
            switch (upscaler)
            {
                case Upscalers.Off: upscaling = false; dlss = false; break;
                case Upscalers.Dlss: upscaling = true; dlss = true; break;
                case Upscalers.Stp: upscaling = true; dlss = false; break;
            }
        }

        private void FollowRenderScale(string renderScale)
        {
            switch (renderScale)
            {
                case RenderScales.Percent100: renderPercentage = 100f; dlssQuality = Dlaa; break;
                case RenderScales.Percent67: renderPercentage = 67f; dlssQuality = DlssQuality; break;
                case RenderScales.Percent58: renderPercentage = 58f; dlssQuality = DlssBalanced; break;
                case RenderScales.Percent50: renderPercentage = 50f; dlssQuality = DlssPerformance; break;
                case RenderScales.Percent33: renderPercentage = 33f; dlssQuality = DlssUltraPerformance; break;
            }
        }

        private void FollowAntialiasing(string mode)
        {
            switch (mode)
            {
                case Antialiasings.Off: antialiasing = HDAdditionalCameraData.AntialiasingMode.None; break;
                case Antialiasings.Fxaa: antialiasing = HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing; break;
                case Antialiasings.Taa: antialiasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing; break;
                case Antialiasings.Smaa: antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing; break;
            }
        }

        // Runs before HDRP sets up each camera's resolution this frame. STP has no camera flag: with upscaling on
        // and DLSS not allowed or absent, HDRP falls to the next upscaler in the pipeline asset, which is STP
        private void ApplyToGameCameras(ScriptableRenderContext context, List<Camera> cameras)
        {
            foreach (Camera view in cameras)
            {
                if (view.cameraType != CameraType.Game || !view.TryGetComponent(out HDAdditionalCameraData cameraData)) continue;

                view.allowDynamicResolution = upscaling;
                cameraData.allowDynamicResolution = upscaling;
                cameraData.allowDeepLearningSuperSampling = dlss;
                cameraData.deepLearningSuperSamplingUseCustomQualitySettings = true;
                cameraData.deepLearningSuperSamplingQuality = dlssQuality;
                cameraData.antialiasing = antialiasing;
            }
        }
    }
}
