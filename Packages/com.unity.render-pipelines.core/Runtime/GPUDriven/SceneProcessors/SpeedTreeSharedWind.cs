#if !UNITY_WEBGL_RENDERER_ONLY
#if ENABLE_TERRAIN_MODULE
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Simulates the wind of a SpeedTree source renderer once per frame and keeps it in one global buffer,
    /// so that any number of copies of the tree read the wind of their source by its index instead of
    /// each copy simulating and uploading its own. Layout of the buffer: per source, the current wind
    /// parameters and then the same parameters of the previous frame, float4 each.
    /// </summary>
    public static class SpeedTreeSharedWind
    {
        public const int ParamsCount = (int)SpeedTreeWindParamIndex.MaxWindParamsCount;

        private const int Float4sPerSource = ParamsCount * 2;
        private const int UintsPerFloat4 = 4;
        private const int UintsPerSource = Float4sPerSource * UintsPerFloat4;

        private static readonly int BufferId = Shader.PropertyToID("_SpeedTreeSharedWind");
        private static readonly List<Renderer> sources = new();
        private static readonly List<bool> simulated = new();
        private static NativeArray<uint> cpuBuffer;
        private static GraphicsBuffer gpuBuffer;
        private static int updatedFrame = -1;
        private static bool hooked;

        public static int Register(Renderer source)
        {
            int index = sources.IndexOf(null);
            if (index < 0)
            {
                index = sources.Count;
                sources.Add(source);
                simulated.Add(false);
            }
            else
            {
                sources[index] = source;
                simulated[index] = false;
            }

            if (!hooked)
            {
                RenderPipelineManager.beginContextRendering += HandleBeginContextRendering;
                hooked = true;
            }

            return index;
        }

        public static void Unregister(int index)
        {
            if (index < 0 || index >= sources.Count)
                return;

            sources[index] = null;
            if (sources.Exists(source => source != null))
                return;

            sources.Clear();
            simulated.Clear();
            Release();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ForgetSourcesOfPreviousPlay()
        {
            sources.Clear();
            simulated.Clear();
            Release();
        }

        private static void Release()
        {
            if (hooked)
            {
                RenderPipelineManager.beginContextRendering -= HandleBeginContextRendering;
                hooked = false;
            }

            if (cpuBuffer.IsCreated)
                cpuBuffer.Dispose();
            gpuBuffer?.Release();
            gpuBuffer = null;
            updatedFrame = -1;
        }

        private static void HandleBeginContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            if (updatedFrame == Time.frameCount)
                return;

            updatedFrame = Time.frameCount;
            EnsureCapacity(sources.Count);

            for (int i = 0; i < sources.Count; i++)
            {
                Renderer source = sources[i];
                if (source == null)
                    continue;

                KeepCurrentAsHistory(i);
                Simulate(source, i);
                if (!simulated[i])
                {
                    KeepCurrentAsHistory(i);
                    simulated[i] = true;
                }
            }

            gpuBuffer.SetData(cpuBuffer);
            Shader.SetGlobalBuffer(BufferId, gpuBuffer);
        }

        private static void EnsureCapacity(int sourceCount)
        {
            int uintCount = Math.Max(1, sourceCount) * UintsPerSource;
            if (cpuBuffer.IsCreated && cpuBuffer.Length >= uintCount)
                return;

            var grown = new NativeArray<uint>(uintCount, Allocator.Persistent);
            if (cpuBuffer.IsCreated)
            {
                NativeArray<uint>.Copy(cpuBuffer, grown, cpuBuffer.Length);
                cpuBuffer.Dispose();
            }

            cpuBuffer = grown;
            gpuBuffer?.Release();
            gpuBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, uintCount / UintsPerFloat4, sizeof(float) * UintsPerFloat4);
        }

        private static void KeepCurrentAsHistory(int index)
        {
            int current = index * UintsPerSource;
            NativeArray<uint>.Copy(cpuBuffer, current, cpuBuffer, current + ParamsCount * UintsPerFloat4, ParamsCount * UintsPerFloat4);
        }

        private static unsafe void Simulate(Renderer source, int index)
        {
            var windParams = new SpeedTreeWindParamsBufferIterator();
            windParams.bufferPtr = (IntPtr)((uint*)cpuBuffer.GetUnsafePtr() + index * UintsPerSource);
            for (int i = 0; i < ParamsCount; i++)
                windParams.uintParamOffsets[i] = i * UintsPerFloat4;
            windParams.uintStride = UintsPerSource;
            windParams.elementOffset = 0;
            windParams.elementsCount = 1;

            EntityId id = source.GetEntityId();
            SpeedTreeWindManager.UpdateWindAndWriteBufferWindParams(new ReadOnlySpan<EntityId>(&id, 1), windParams, false);
        }
    }
}
#endif

#endif // !UNITY_WEBGL_RENDERER_ONLY
