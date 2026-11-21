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
        private readonly Dictionary<Camera, AccelerationStructureBuilder> builders = new();
        private readonly List<Camera> forgottenCameras = new();

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            profile = ScriptableObject.CreateInstance<VolumeProfile>();

            rayTracingSettings = profile.Add<RayTracingSettings>();
            rayTracingSettings.buildMode.Override(RTASBuildMode.Manual);
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
            pathTracing.maximumSamples.Override(1);

            GetComponent<Volume>().sharedProfile = profile;
        }

        private void OnEnable()
        {
            clientConfig = Config.GetGameConfig().Client;
            clientConfig.propertyChanged += HandleClientConfigChanged;
            Registers.Instance.SubscribeRegistered<HDAdditionalLightData>(HandleLightRegistered);
            Registers.Instance.SubscribeRegistered<WindZone>(HandleWindRegistered);
            Registers.Instance.SubscribeRegistered<PathTracingOnly>(HandlePathTracingOnlyRegistered);
            RenderPipelineManager.beginCameraRendering += BuildAccelerationStructure;
            FollowClientConfig();
        }

        private void OnDisable()
        {
            clientConfig.propertyChanged -= HandleClientConfigChanged;
            Registers.Instance.UnsubscribeRegistered<HDAdditionalLightData>(HandleLightRegistered);
            Registers.Instance.UnsubscribeRegistered<WindZone>(HandleWindRegistered);
            Registers.Instance.UnsubscribeRegistered<PathTracingOnly>(HandlePathTracingOnlyRegistered);
            RenderPipelineManager.beginCameraRendering -= BuildAccelerationStructure;
        }

        private void OnDestroy()
        {
            foreach (AccelerationStructureBuilder builder in builders.Values)
            {
                builder.Dispose();
            }

            builders.Clear();

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

        // Traced trees stand still: their wind lives in the vertex shader, which the acceleration structure does not see
        private void HandleWindRegistered(WindZone wind)
        {
            bool blowing = !IsTracingAnything();
            ShooterEntity.SetEnabled(wind, blowing);
            Log.Info($"{wind.name} registered: {(blowing ? "blowing" : "calmed")}");
        }

        private void HandlePathTracingOnlyRegistered(PathTracingOnly stand)
        {
            bool shown = clientConfig.PathTracing == Toggles.On;
            ShowPathTracingOnly(stand, shown);
            Log.Info($"{stand.name} registered: {(shown ? "shown" : "hidden")}");
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

            bool blowing = !IsTracingAnything();
            foreach (WindZone wind in Registers.Instance.GetAll<WindZone>(RegisterInactivePolicy.Include))
            {
                ShooterEntity.SetEnabled(wind, blowing);
            }

            bool pathTraced = clientConfig.PathTracing == Toggles.On;
            foreach (PathTracingOnly stand in Registers.Instance.GetAll<PathTracingOnly>(RegisterInactivePolicy.Include))
            {
                ShowPathTracingOnly(stand, pathTraced);
            }

            Log.Info($"Ray tracing: global illumination {clientConfig.RayTracedGlobalIllumination}, reflections {clientConfig.RayTracedReflections}, " +
                     $"ambient occlusion {clientConfig.RayTracedAmbientOcclusion}, sun shadows {clientConfig.RayTracedSunShadows}, lamp shadows {clientConfig.RayTracedLampShadows}, " +
                     $"subsurface scattering {clientConfig.RayTracedSubsurfaceScattering}, recursive rendering {clientConfig.RecursiveRendering}, path tracing {clientConfig.PathTracing}; " +
                     $"{rayTracedShadows} lights with ray traced shadows, wind {(blowing ? "blowing" : "calmed")}");
        }

        // Manual build: HDRP's own one has no place for instances that are not renderers, like terrain trees
        private void BuildAccelerationStructure(ScriptableRenderContext context, Camera camera)
        {
            HDCamera hdCamera = HDCamera.GetOrCreate(camera);
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing)) return;

            if (!builders.TryGetValue(camera, out AccelerationStructureBuilder builder))
            {
                ForgetDestroyedCameras();
                builder = new AccelerationStructureBuilder();
                builders[camera] = builder;
            }

            bool rayTracedShadows = clientConfig.RayTracedSunShadows == Toggles.On || clientConfig.RayTracedLampShadows == Toggles.On;
            if (!builder.TryCull(hdCamera, rayTracedShadows))
            {
                hdCamera.rayTracingAccelerationStructure = null;
                return;
            }

            foreach (IRayTracedInstances instances in Registers.Instance.GetAll<IRayTracedInstances>(RegisterInactivePolicy.Exclude))
            {
                instances.AddInstancesTo(builder, hdCamera);
            }

            hdCamera.rayTracingAccelerationStructure = builder.Build(hdCamera);
            if (clientConfig.PathTracing == Toggles.On) (RenderPipelineManager.currentPipeline as HDRenderPipeline)?.ResetPathTracing(hdCamera);
        }

        private void ForgetDestroyedCameras()
        {
            forgottenCameras.Clear();
            foreach (KeyValuePair<Camera, AccelerationStructureBuilder> pair in builders)
            {
                if (pair.Key == null) forgottenCameras.Add(pair.Key);
            }

            foreach (Camera camera in forgottenCameras)
            {
                builders[camera].Dispose();
                builders.Remove(camera);
            }
        }

        private bool IsTracingAnything()
        {
            return clientConfig.RayTracedGlobalIllumination != RayTracingModes.Off
                   || clientConfig.RayTracedReflections != RayTracingModes.Off
                   || clientConfig.RayTracedAmbientOcclusion == Toggles.On
                   || clientConfig.RayTracedSunShadows == Toggles.On
                   || clientConfig.RayTracedLampShadows == Toggles.On
                   || clientConfig.RayTracedSubsurfaceScattering == Toggles.On
                   || clientConfig.RecursiveRendering == Toggles.On
                   || clientConfig.PathTracing == Toggles.On;
        }

        private static void ShowPathTracingOnly(PathTracingOnly stand, bool shown)
        {
            ShooterEntity.SetEnabled(stand.GetComponent<Renderer>(), shown);
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
