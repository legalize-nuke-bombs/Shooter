using System;
using System.Reflection;
using Shooter.Infrastructure;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shooter.Editor
{
    // Unity's own sky occlusion, the one that handles the whole map on the ray tracing cores, run from positions with the stuck probes pushed out;
    // Unity keeps that class internal, so it is created by reflection and gets the bake jobs Unity would have handed it itself.
    public class PushedSkyOcclusionBaker : AdaptiveProbeVolumes.SkyOcclusionBaker
    {
        private const BindingFlags AnyInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly Journal Log = Logs.Here();
        private static readonly Type UnitySkyOcclusionType = typeof(AdaptiveProbeVolumes).GetNestedType("DefaultSkyOcclusion", BindingFlags.NonPublic);
        private static readonly FieldInfo BakeDataField = typeof(AdaptiveProbeVolumes).GetField("s_BakeData", BindingFlags.NonPublic | BindingFlags.Static);

        private AdaptiveProbeVolumes.SkyOcclusionBaker unitySkyOcclusion;
        private NativeArray<Vector3> pushedPositions;

        public override ulong currentStep => unitySkyOcclusion.currentStep;
        public override ulong stepCount => unitySkyOcclusion.stepCount;
        public override NativeArray<Vector4> occlusion => unitySkyOcclusion.occlusion;
        public override NativeArray<Vector3> shadingDirections => unitySkyOcclusion.shadingDirections;

        public override void Initialize(ProbeVolumeBakingSet bakingSet, NativeArray<Vector3> probePositions)
        {
            Vector3[] pushed = StuckProbes.PushOut(probePositions, bakingSet.minDistanceBetweenProbes * 0.5f, out int pushedCount, out int stuckCount);
            pushedPositions = new NativeArray<Vector3>(pushed, Allocator.Persistent);
            Log.Info($"Sky occlusion: {pushedCount} of {pushed.Length} probes pushed out of walls and the ground, {stuckCount} stay stuck");

            unitySkyOcclusion = (AdaptiveProbeVolumes.SkyOcclusionBaker)Activator.CreateInstance(UnitySkyOcclusionType, true);
            object bakeData = BakeDataField.GetValue(null);
            object jobs = bakeData.GetType().GetField("jobs", AnyInstance).GetValue(bakeData);
            UnitySkyOcclusionType.GetField("jobs", AnyInstance).SetValue(unitySkyOcclusion, jobs);
            unitySkyOcclusion.Initialize(bakingSet, pushedPositions);
        }

        public override bool Step()
        {
            return unitySkyOcclusion.Step();
        }

        public override void Dispose()
        {
            unitySkyOcclusion?.Dispose();
            if (pushedPositions.IsCreated) pushedPositions.Dispose();
        }
    }
}
