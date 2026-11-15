using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shooter.Editor
{
    // The sky reaches the probes through sky occlusion and every lamp of ours is live, so the lighting step has nothing to bake;
    // Unity's own step would still hand the whole map to the lightmapper, which runs out of video memory on it.
    // Validity comes from StuckProbes, which the sky step runs before this one.
    public class SkyOnlyLightingBaker : AdaptiveProbeVolumes.LightingBaker
    {
        private NativeArray<SphericalHarmonicsL2> irradianceResults;
        private NativeArray<float> validityResults;
        private NativeArray<Vector4> occlusionResults;
        private bool done;

        public override ulong currentStep => done ? 1UL : 0UL;
        public override ulong stepCount => 1UL;
        public override NativeArray<SphericalHarmonicsL2> irradiance => irradianceResults;
        public override NativeArray<float> validity => validityResults;
        public override NativeArray<Vector4> occlusion => occlusionResults;

        public override void Initialize(bool bakeProbeOcclusion, NativeArray<Vector3> probePositions)
        {
            Allocate(probePositions.Length);
        }

        public override void Initialize(bool bakeProbeOcclusion, NativeArray<Vector3> probePositions, NativeArray<uint> bakedRenderingLayerMasks)
        {
            Allocate(probePositions.Length);
        }

        public override bool Step()
        {
            done = true;
            return true;
        }

        public override void Dispose()
        {
            if (irradianceResults.IsCreated) irradianceResults.Dispose();
            if (validityResults.IsCreated) validityResults.Dispose();
            if (occlusionResults.IsCreated) occlusionResults.Dispose();
        }

        private void Allocate(int probeCount)
        {
            Dispose();
            irradianceResults = new NativeArray<SphericalHarmonicsL2>(probeCount, Allocator.Persistent);
            validityResults = new NativeArray<float>(probeCount, Allocator.Persistent);
            occlusionResults = new NativeArray<Vector4>(probeCount, Allocator.Persistent);

            float[] stuckValidity = StuckProbes.TakeLastValidity();
            if (stuckValidity != null)
            {
                for (int i = 0; i < probeCount && i < stuckValidity.Length; i++)
                {
                    validityResults[i] = stuckValidity[i];
                }
            }

            done = false;
        }
    }
}
