using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shooter.Client.Interface.Overlays
{
    public class FrameProfile
    {
        private static readonly Journal Log = Logs.Here();

        private const int Shown = 20;
        private const int TrimAfter = 40;

        private const string PassIds =
            "UnityEngine.Rendering.HighDefinition.HDProfileId, Unity.RenderPipelines.HighDefinition.Runtime";

        private readonly List<ProfilingSampler> samplers = new();
        private readonly List<float> peaks = new();
        private readonly List<Measured> ranked = new();
        private readonly FrameTiming[] timings = new FrameTiming[1];
        private readonly StringBuilder builder = new();

        private int samples;

        public bool Listening => samplers.Count > 0;

        // The pipeline keeps a sampler per pass, but the identifiers of those passes are internal to
        // HDRP, and its own panel names nineteen of them by hand. The samplers themselves are public,
        // so the whole set is taken through the enumeration and switched on at once.
        public void Listen()
        {
            Forget();

            Type ids = Type.GetType(PassIds);
            if (ids == null)
            {
                Log.Error("Pipeline pass identifiers are not where they used to be, the frame page stays empty");
                return;
            }

            MethodInfo get = typeof(ProfilingSampler)
                .GetMethod(nameof(ProfilingSampler.Get), BindingFlags.Public | BindingFlags.Static)
                ?.MakeGenericMethod(ids);

            if (get == null)
            {
                Log.Error("Profiling samplers can not be reached, the frame page stays empty");
                return;
            }

            var argument = new object[1];
            foreach (object id in Enum.GetValues(ids))
            {
                argument[0] = id;

                if (get.Invoke(null, argument) is not ProfilingSampler sampler) continue;

                sampler.enableRecording = true;
                samplers.Add(sampler);
                peaks.Add(0f);
            }

            samples = 0;
            Log.Info("Frame profile records {} pipeline passes", samplers.Count);
        }

        public void Forget()
        {
            foreach (ProfilingSampler sampler in samplers) sampler.enableRecording = false;

            samplers.Clear();
            peaks.Clear();
        }

        public string Report()
        {
            builder.Clear();

            Frame();

            ranked.Clear();
            for (int i = 0; i < samplers.Count; i++)
            {
                float gpu = samplers[i].gpuElapsedTime;
                float cpu = samplers[i].cpuElapsedTime + samplers[i].inlineCpuElapsedTime;

                if (gpu > peaks[i]) peaks[i] = gpu;
                if (gpu > 0f || cpu > 0f) ranked.Add(new Measured(samplers[i].name, gpu, cpu));
            }

            if (++samples == TrimAfter) Trim();

            if (ranked.Count == 0)
            {
                builder.Append("Ни один проход не отчитался: сборка не отладочная или конвейер ещё не рисовал");

                return builder.ToString();
            }

            ranked.Sort((left, right) => right.Gpu.CompareTo(left.Gpu));

            builder.Append("GPU      CPU      Проход\n");

            int taken = Mathf.Min(Shown, ranked.Count);
            float gpuShown = 0f;
            for (int i = 0; i < taken; i++)
            {
                gpuShown += ranked[i].Gpu;
                builder.Append(ranked[i].Gpu.ToString("F2").PadRight(9))
                    .Append(ranked[i].Cpu.ToString("F2").PadRight(9))
                    .Append(ranked[i].Name).Append('\n');
            }

            float gpuRest = 0f;
            for (int i = taken; i < ranked.Count; i++) gpuRest += ranked[i].Gpu;

            builder.Append("Остальные ").Append(ranked.Count - taken).Append(" прохода ")
                .Append(gpuRest.ToString("F2")).Append(" мс\n")
                .Append("Показано ").Append(gpuShown.ToString("F2")).Append(" мс из ").Append(samplers.Count)
                .Append(" записываемых; вложенные входят и в родителя");

            return builder.ToString();
        }

        // Every recorded pass costs a pair of gpu timestamps per frame, and two hundred of those are
        // paid for nothing: most passes never run in our scene. Once it is clear which ones stay silent,
        // their recording is switched off and the panel stops distorting what it measures.
        private void Trim()
        {
            int before = samplers.Count;

            for (int i = samplers.Count - 1; i >= 0; i--)
            {
                if (peaks[i] > 0f) continue;

                samplers[i].enableRecording = false;
                samplers.RemoveAt(i);
                peaks.RemoveAt(i);
            }

            Log.Info("Frame profile stops recording {} silent passes, {} left", before - samplers.Count,
                samplers.Count);
        }

        private void Frame()
        {
            FrameTimingManager.CaptureFrameTimings();

            if (FrameTimingManager.GetLatestTimings(1, timings) == 0)
            {
                builder.Append("Время кадра недоступно: включи Frame Timing Stats в настройках проигрывателя\n\n");

                return;
            }

            FrameTiming frame = timings[0];
            builder.Append("Кадр ").Append(frame.cpuFrameTime.ToString("F2"))
                .Append(" мс   GPU ").Append(frame.gpuFrameTime.ToString("F2"))
                .Append(" мс   Главный поток ").Append(frame.cpuMainThreadFrameTime.ToString("F2"))
                .Append(" мс   Ожидание вывода ").Append(frame.cpuMainThreadPresentWaitTime.ToString("F2"))
                .Append(" мс\n\n");
        }

        private readonly struct Measured
        {
            public Measured(string name, float gpu, float cpu)
            {
                Name = name;
                Gpu = gpu;
                Cpu = cpu;
            }

            public string Name { get; }

            public float Gpu { get; }

            public float Cpu { get; }
        }
    }
}
