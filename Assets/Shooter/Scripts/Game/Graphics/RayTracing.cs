using System.Collections.Generic;
using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Volume))]
    public class RayTracing : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly HashSet<string> FollowedProperties = new()
        {
            nameof(ClientConfig.RayTracedGlobalIllumination),
            nameof(ClientConfig.RayTracedReflections),
            nameof(ClientConfig.RayTracedAmbientOcclusion),
            nameof(ClientConfig.RayTracedSunShadows),
            nameof(ClientConfig.RayTracedLampShadows),
            nameof(ClientConfig.RayTracedSubsurfaceScattering),
            nameof(ClientConfig.RecursiveRendering),
            nameof(ClientConfig.PathTracing)
        };

        private ClientConfig clientConfig;
        private VolumeProfile profile;
        private RayTracingSettings rayTracingSettings;
        private GlobalIllumination globalIllumination;
        private ScreenSpaceReflection reflections;
        private ScreenSpaceAmbientOcclusion ambientOcclusion;
        private SubSurfaceScattering subsurfaceScattering;
        private RecursiveRendering recursiveRendering;
        private PathTracing pathTracing;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            profile = ScriptableObject.CreateInstance<VolumeProfile>();

            rayTracingSettings = profile.Add<RayTracingSettings>();
            rayTracingSettings.cullingMode.Override(RTASCullingMode.ExtendedFrustum);
            rayTracingSettings.extendShadowCulling.Override(true);
            rayTracingSettings.extendCameraCulling.Override(true);

            globalIllumination = profile.Add<GlobalIllumination>();
            globalIllumination.enable.Override(true);
            globalIllumination.mode.Override(RayTracingMode.Performance);

            reflections = profile.Add<ScreenSpaceReflection>();
            reflections.enabled.Override(true);
            reflections.enabledTransparent.Override(true);
            reflections.mode.Override(RayTracingMode.Performance);

            ambientOcclusion = profile.Add<ScreenSpaceAmbientOcclusion>();
            ambientOcclusion.rayTracing.Override(true);

            subsurfaceScattering = profile.Add<SubSurfaceScattering>();
            subsurfaceScattering.rayTracing.Override(true);

            recursiveRendering = profile.Add<RecursiveRendering>();
            recursiveRendering.enable.Override(true);

            pathTracing = profile.Add<PathTracing>();
            pathTracing.enable.Override(true);

            GetComponent<Volume>().sharedProfile = profile;
        }

        private void OnEnable()
        {
            clientConfig = Config.GetGameConfig().Client;
            clientConfig.propertyChanged += HandleClientConfigChanged;
            Registers.Instance.SubscribeRegistered<HDAdditionalLightData>(HandleLightRegistered);
            FollowClientConfig();
        }

        private void OnDisable()
        {
            clientConfig.propertyChanged -= HandleClientConfigChanged;
            Registers.Instance.UnsubscribeRegistered<HDAdditionalLightData>(HandleLightRegistered);
        }

        private void OnDestroy()
        {
            foreach (VolumeComponent component in profile.components)
            {
                Destroy(component);
            }

            Destroy(profile);
        }

        private void HandleClientConfigChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            if (!FollowedProperties.Contains(args.propertyName)) return;

            FollowClientConfig();
        }

        private void HandleLightRegistered(HDAdditionalLightData light)
        {
            Log.Info($"{light.name} registered: {UpdateShadowTracing(light)}");
        }

        private void FollowClientConfig()
        {
            FollowMode(globalIllumination, globalIllumination.tracing, clientConfig.RayTracedGlobalIllumination);
            FollowMode(reflections, reflections.tracing, clientConfig.RayTracedReflections);
            ambientOcclusion.active = clientConfig.RayTracedAmbientOcclusion == Toggles.On;
            subsurfaceScattering.active = clientConfig.RayTracedSubsurfaceScattering == Toggles.On;
            recursiveRendering.active = clientConfig.RecursiveRendering == Toggles.On;
            pathTracing.active = clientConfig.PathTracing == Toggles.On;

            int rayTracedShadows = 0;
            foreach (HDAdditionalLightData light in Registers.Instance.GetAll<HDAdditionalLightData>(RegisterInactivePolicy.Include))
            {
                if (UpdateShadowTracing(light) == ShadowTracing.RayTraced) rayTracedShadows++;
            }

            Log.Info($"Ray tracing: global illumination {clientConfig.RayTracedGlobalIllumination}, reflections {clientConfig.RayTracedReflections}, " +
                     $"ambient occlusion {clientConfig.RayTracedAmbientOcclusion}, sun shadows {clientConfig.RayTracedSunShadows}, lamp shadows {clientConfig.RayTracedLampShadows}, " +
                     $"subsurface scattering {clientConfig.RayTracedSubsurfaceScattering}, recursive rendering {clientConfig.RecursiveRendering}, path tracing {clientConfig.PathTracing}; " +
                     $"{rayTracedShadows} lights with ray traced shadows");
        }

        private static void FollowMode(VolumeComponent component, RayCastingModeParameter tracing, string mode)
        {
            component.active = mode != RayTracingModes.Off;
            tracing.Override(mode == RayTracingModes.Mixed ? RayCastingMode.Mixed : RayCastingMode.RayTracing);
        }

        private ShadowTracing UpdateShadowTracing(HDAdditionalLightData light)
        {
            Light unityLight = light.GetComponent<Light>();
            if (unityLight.shadows == LightShadows.None) return ShadowTracing.NoShadows;

            bool directional = unityLight.type == LightType.Directional;
            bool rayTraced = (directional ? clientConfig.RayTracedSunShadows : clientConfig.RayTracedLampShadows) == Toggles.On;
            if (directional) light.useScreenSpaceShadows = rayTraced;
            light.useRayTracedShadows = rayTraced;
            return rayTraced ? ShadowTracing.RayTraced : ShadowTracing.ShadowMap;
        }

        private enum ShadowTracing
        {
            NoShadows,
            ShadowMap,
            RayTraced
        }
    }
}
