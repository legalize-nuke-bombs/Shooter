using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    [RequireComponent(typeof(Camera))]
    [RequireComponent(typeof(HDAdditionalCameraData))]
    public class CameraAntialiasing : MonoBehaviour
    {
        // DLSS quality codes as the NVIDIA module numbers them; the camera keeps them as a plain number
        private const uint DlssPerformance = 0;
        private const uint DlssBalanced = 1;
        private const uint DlssQuality = 2;
        private const uint Dlaa = 4;
        private static readonly Journal Log = Logs.Here();

        private Camera view;
        private HDAdditionalCameraData additionalCameraData;
        private ClientConfig clientConfig;

        private void Awake()
        {
            view = GetComponent<Camera>();
            additionalCameraData = GetComponent<HDAdditionalCameraData>();
        }

        private void OnEnable()
        {
            clientConfig = Config.GetGameConfig().Client;
            clientConfig.propertyChanged += HandleClientConfigChanged;
            ApplyAntialiasingAndUpscaler();
        }

        private void OnDisable()
        {
            clientConfig.propertyChanged -= HandleClientConfigChanged;
        }

        private void HandleClientConfigChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            ApplyAntialiasingAndUpscaler();
        }

        private void ApplyAntialiasingAndUpscaler()
        {
            ApplyAntialiasing(clientConfig.Antialiasing);
            ApplyUpscaler(clientConfig.Upscaler);
        }

        private void ApplyAntialiasing(string mode)
        {
            HDAdditionalCameraData.AntialiasingMode antialiasing;
            switch (mode)
            {
                case Antialiasings.Off: antialiasing = HDAdditionalCameraData.AntialiasingMode.None; break;
                case Antialiasings.Fxaa: antialiasing = HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing; break;
                case Antialiasings.Taa: antialiasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing; break;
                case Antialiasings.Smaa: antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing; break;
                default:
                    Log.Warn($"Entity {name} does not know antialiasing {mode}, the view keeps its own");
                    return;
            }

            additionalCameraData.antialiasing = antialiasing;
            Log.Info($"Entity {name} antialiasing is {mode}");
        }

        private void ApplyUpscaler(string mode)
        {
            uint dlssQuality;
            switch (mode)
            {
                case Upscalers.Off: dlssQuality = 0; break;
                case Upscalers.Dlaa: dlssQuality = Dlaa; break;
                case Upscalers.Quality: dlssQuality = DlssQuality; break;
                case Upscalers.Balanced: dlssQuality = DlssBalanced; break;
                case Upscalers.Performance: dlssQuality = DlssPerformance; break;
                default:
                    Log.Warn($"Entity {name} does not know upscaler {mode}, the view keeps its own");
                    return;
            }

            // HDRP hands the view to DLSS only when both flags allow it; off returns it to its own antialiasing
            bool useDlss = mode != Upscalers.Off;
            view.allowDynamicResolution = useDlss;
            additionalCameraData.allowDynamicResolution = useDlss;
            additionalCameraData.allowDeepLearningSuperSampling = useDlss;
            additionalCameraData.deepLearningSuperSamplingUseCustomQualitySettings = useDlss;
            additionalCameraData.deepLearningSuperSamplingUseOptimalSettings = true;
            additionalCameraData.deepLearningSuperSamplingQuality = dlssQuality;

            Log.Info($"Entity {name} upscaler is {mode}");
        }
    }
}
