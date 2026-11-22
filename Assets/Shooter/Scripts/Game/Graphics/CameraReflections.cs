using System.Collections.Generic;
using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public class CameraReflections : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly HashSet<string> FollowedProperties = new()
        {
            nameof(ClientConfig.Reflections),
            nameof(ClientConfig.RayTracedReflections)
        };

        private ClientConfig clientConfig;
        private bool opaqueScreenSpace;
        private bool transparentScreenSpace;
        private bool planarProbes;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            clientConfig = Config.GetGameConfig().Client;
            clientConfig.propertyChanged += HandleClientConfigChanged;
            Registers.Instance.SubscribeRegistered<PlanarReflectionProbe>(HandleProbeRegistered);
            RenderPipelineManager.beginContextRendering += ApplyToGameCameras;
            FollowClientConfig();
        }

        private void OnDisable()
        {
            clientConfig.propertyChanged -= HandleClientConfigChanged;
            Registers.Instance.UnsubscribeRegistered<PlanarReflectionProbe>(HandleProbeRegistered);
            RenderPipelineManager.beginContextRendering -= ApplyToGameCameras;
        }

        private void HandleClientConfigChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            if (!FollowedProperties.Contains(args.propertyName)) return;

            FollowClientConfig();
        }

        private void HandleProbeRegistered(PlanarReflectionProbe probe)
        {
            ShooterEntity.SetEnabled(probe, planarProbes);
            Log.Info($"{probe.name} registered: {(planarProbes ? "reflecting" : "off")}");
        }

        // Ray traced reflections go through the screen space reflection pass, so while they are on they own every surface
        private void FollowClientConfig()
        {
            bool rayTraced = clientConfig.RayTracedReflections != RayTracingModes.Off;
            opaqueScreenSpace = rayTraced || clientConfig.Reflections != ReflectionModes.Off;
            transparentScreenSpace = rayTraced || clientConfig.Reflections == ReflectionModes.ScreenSpace;
            planarProbes = !rayTraced && clientConfig.Reflections == ReflectionModes.Planar;

            int probes = 0;
            foreach (PlanarReflectionProbe probe in Registers.Instance.GetAll<PlanarReflectionProbe>(RegisterInactivePolicy.Include))
            {
                ShooterEntity.SetEnabled(probe, planarProbes);
                probes++;
            }

            Log.Info($"Reflections {clientConfig.Reflections}, ray traced {clientConfig.RayTracedReflections}: screen space on opaque {opaqueScreenSpace}, " +
                     $"on transparent {transparentScreenSpace}, {probes} planar probes {(planarProbes ? "reflecting" : "off")}");
        }

        private void ApplyToGameCameras(ScriptableRenderContext context, List<Camera> cameras)
        {
            foreach (Camera view in cameras)
            {
                if (view.cameraType != CameraType.Game || !view.TryGetComponent(out HDAdditionalCameraData cameraData)) continue;

                cameraData.customRenderingSettings = true;
                cameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.SSR] = true;
                cameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.TransparentSSR] = true;
                cameraData.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.SSR, opaqueScreenSpace);
                cameraData.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.TransparentSSR, transparentScreenSpace);
            }
        }
    }
}
