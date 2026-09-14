using Shooter.Configuring;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace Shooter.Client.Playing
{
    [RequireComponent(typeof(Camera))]
    [RequireComponent(typeof(HDAdditionalCameraData))]
    public class ViewSettings : MonoBehaviour
    {
        // DLSS quality codes as the NVIDIA module numbers them; the camera keeps them as a plain number
        private const uint DlssPerformance = 0;
        private const uint DlssBalanced = 1;
        private const uint DlssQuality = 2;
        private const uint Dlaa = 4;
        private static readonly Journal Log = Logs.Here();

        private Camera view;
        private HDAdditionalCameraData data;
        private ClientConfig client;

        private void Awake()
        {
            view = GetComponent<Camera>();
            data = GetComponent<HDAdditionalCameraData>();
        }

        private void OnEnable()
        {
            client = Config.Read().Client;
            client.propertyChanged += Changed;
            Apply();
        }

        private void OnDisable()
        {
            client.propertyChanged -= Changed;
        }

        private void Changed(object sender, BindablePropertyChangedEventArgs args)
        {
            Apply();
        }

        private void Apply()
        {
            ApplyAntialiasing(client.Antialiasing);
            ApplyUpscaler(client.Upscaler);
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

            data.antialiasing = antialiasing;
            Log.Info($"Entity {name} antialiasing is {mode}");
        }

        private void ApplyUpscaler(string mode)
        {
            uint quality;
            switch (mode)
            {
                case Upscalers.Off: quality = 0; break;
                case Upscalers.Dlaa: quality = Dlaa; break;
                case Upscalers.Quality: quality = DlssQuality; break;
                case Upscalers.Balanced: quality = DlssBalanced; break;
                case Upscalers.Performance: quality = DlssPerformance; break;
                default:
                    Log.Warn($"Entity {name} does not know upscaler {mode}, the view keeps its own");
                    return;
            }

            // HDRP hands the view to DLSS only when both flags allow it; off returns it to its own antialiasing
            bool dlss = mode != Upscalers.Off;
            view.allowDynamicResolution = dlss;
            data.allowDynamicResolution = dlss;
            data.allowDeepLearningSuperSampling = dlss;
            data.deepLearningSuperSamplingUseCustomQualitySettings = dlss;
            data.deepLearningSuperSamplingUseOptimalSettings = true;
            data.deepLearningSuperSamplingQuality = quality;

            Log.Info($"Entity {name} upscaler is {mode}");
        }
    }
}
