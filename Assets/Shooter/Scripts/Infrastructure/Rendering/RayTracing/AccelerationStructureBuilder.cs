using System;
using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.Rendering.HighDefinition
{
    public sealed class AccelerationStructureBuilder : IDisposable
    {
        private const string AlphaTestKeyword = "_ALPHATEST_ON";
        private const string DoubleSidedKeyword = "_DOUBLESIDED_ON";
        private const string TransparentKeyword = "_SURFACE_TYPE_TRANSPARENT";

        private readonly HDRTASManager manager = new();
        private readonly Dictionary<EntityId, int> materialCrcs = new();
        private readonly List<Material> materials = new();

        public AccelerationStructureBuilder()
        {
            manager.Initialize();
            manager.Reset();
        }

        public bool TryCull(HDCamera hdCamera, bool rayTracedShadows)
        {
            manager.Reset();
            HDEffectsParameters parameters = HDRenderPipeline.EvaluateEffectsParameters(hdCamera, rayTracedShadows, false);
            if (!parameters.rayTracingRequired) return false;

            RayTracingInstanceCullingResults results = manager.Cull(hdCamera, parameters);
            if (parameters.pathTracing) TrackPathTracingChanges(hdCamera, results);
            return true;
        }

        public void AddInstances(Renderer source, Mesh mesh, NativeArray<Matrix4x4> objectToWorld)
        {
            source.GetSharedMaterials(materials);
            uint mask = MaskOf(source.shadowCastingMode);
            for (int subMesh = 0; subMesh < mesh.subMeshCount && subMesh < materials.Count; subMesh++)
            {
                Material material = materials[subMesh];
                if (material == null) continue;

                bool transparent = material.IsKeywordEnabled(TransparentKeyword);
                RayTracingMeshInstanceConfig config = new()
                {
                    mesh = mesh,
                    subMeshIndex = (uint)subMesh,
                    material = material,
                    subMeshFlags = SubMeshFlagsOf(material, transparent),
                    enableTriangleCulling = !material.IsKeywordEnabled(DoubleSidedKeyword),
                    frontTriangleCounterClockwise = false,
                    layer = source.gameObject.layer,
                    renderingLayerMask = source.renderingLayerMask,
                    mask = transparent ? mask & ~(uint)RayTracingRendererFlag.CastShadowOpaque : mask,
                    motionVectorMode = MotionVectorGenerationMode.Camera,
                    lightProbeUsage = source.lightProbeUsage
                };
                manager.rtas.AddInstances(in config, objectToWorld);
            }
        }

        public RayTracingAccelerationStructure Build(HDCamera hdCamera)
        {
            manager.Build(hdCamera);
            return manager.rtas;
        }

        public void Dispose()
        {
            manager.ReleaseResources();
        }

        private void TrackPathTracingChanges(HDCamera hdCamera, RayTracingInstanceCullingResults results)
        {
            hdCamera.transformsDirty |= results.transformsChanged;
            foreach (RayTracingInstanceMaterialCRC material in results.materialsCRC)
            {
                if (materialCrcs.TryGetValue(material.entityId, out int crc) && crc == material.crc) continue;

                materialCrcs[material.entityId] = material.crc;
                hdCamera.materialsDirty = true;
            }
        }

        private static uint MaskOf(ShadowCastingMode shadowCasting)
        {
            return shadowCasting switch
            {
                ShadowCastingMode.Off => (uint)(RayTracingRendererFlag.All & ~RayTracingRendererFlag.CastShadow),
                ShadowCastingMode.ShadowsOnly => (uint)RayTracingRendererFlag.CastShadow,
                _ => (uint)RayTracingRendererFlag.All
            };
        }

        private static RayTracingSubMeshFlags SubMeshFlagsOf(Material material, bool transparent)
        {
            if (transparent) return RayTracingSubMeshFlags.Enabled | RayTracingSubMeshFlags.UniqueAnyHitCalls;

            RenderQueueRange alphaTest = HDRenderQueue.k_RenderQueue_OpaqueAlphaTest;
            bool alphaTested = material.IsKeywordEnabled(AlphaTestKeyword) || (material.renderQueue >= alphaTest.lowerBound && material.renderQueue <= alphaTest.upperBound);
            return alphaTested ? RayTracingSubMeshFlags.Enabled : RayTracingSubMeshFlags.Enabled | RayTracingSubMeshFlags.ClosestHitOnly;
        }
    }
}
