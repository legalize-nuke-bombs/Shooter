using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static System.FormattableString;

namespace Shooter.Infrastructure
{
    public class DebugOverlay : Overlay, IShooterUpdateable
    {
        private const string PanelLabelName = "debug";
        private const int FrameSamples = 120;
        private const int LargestClassCount = 5;
        private const int LeftColumnWidth = 34;
        private const float PanelRefreshSeconds = 0.25f;
        private const float LogReportSeconds = 60f;
        private const float Megabyte = 1024f * 1024f;
        private const string Unavailable = "—";
        private static readonly Journal Log = Logs.Here();

        private static readonly (ProfilerCategory Category, string Name)[] ProfilerCounters =
        {
            (ProfilerCategory.Memory, "System Used Memory"),
            (ProfilerCategory.Memory, "Total Reserved Memory"),
            (ProfilerCategory.Memory, "GC Used Memory"),
            (ProfilerCategory.Memory, "Gfx Used Memory"),
            (ProfilerCategory.Render, "Batches Count"),
            (ProfilerCategory.Render, "Triangles Count"),
            (ProfilerCategory.Render, "Vertices Count")
        };

        private static readonly string[] CompassDirections =
        {
            "north", "north-east", "east", "south-east", "south", "south-west", "west", "north-west"
        };

        private readonly StringBuilder panelTextBuilder = new();
        private readonly float[] frameDurations = new float[FrameSamples];

        private readonly Dictionary<string, ProfilerRecorder> recordersByCounterName = new();
        private readonly Type[] largestClasses = new Type[LargestClassCount];
        private readonly int[] largestClassCounts = new int[LargestClassCount];
        private int sampledFrameCount;
        private int nextFrameIndex;

        private Label panelLabel;
        private float secondsToPanelRefresh;
        private float secondsToLogReport = LogReportSeconds;
        private bool visible;

        protected override void ShooterAwake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void ShooterUpdate()
        {
            if (!IsBound) return;

            SampleFrameDuration();

            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame) TogglePanel();

            secondsToLogReport -= Time.unscaledDeltaTime;
            if (secondsToLogReport <= 0)
            {
                secondsToLogReport = LogReportSeconds;
                Log.Info($"FPS {FormatFrameRate()}, VRAM {FormatVideoMemory()}, RAM {FormatCounterMegabytes("System Used Memory")}, batches {FormatCounterCount("Batches Count")}, triangles {FormatCounterCount("Triangles Count")}");
            }

            if (!visible) return;

            secondsToPanelRefresh -= Time.unscaledDeltaTime;
            if (secondsToPanelRefresh > 0) return;

            secondsToPanelRefresh = PanelRefreshSeconds;
            panelLabel.text = BuildPanelText();
        }

        protected override bool TryBindElements(VisualElement root)
        {
            panelLabel = root.Q<Label>(PanelLabelName);

            if (panelLabel == null)
            {
                Log.Error($"Overlay document has no {PanelLabelName} label, the debug panel stays hidden");
                return false;
            }

            visible = false;
            nextFrameIndex = sampledFrameCount = 0;
            secondsToPanelRefresh = 0;
            panelLabel.style.display = DisplayStyle.None;
            StartProfilerRecorders();

            return true;
        }

        protected override void UnbindElements()
        {
            foreach (ProfilerRecorder recorder in recordersByCounterName.Values)
            {
                recorder.Dispose();
            }

            recordersByCounterName.Clear();
            panelLabel = null;
        }

        private void StartProfilerRecorders()
        {
            foreach ((ProfilerCategory category, string counterName) in ProfilerCounters)
            {
                ProfilerRecorder recorder = ProfilerRecorder.StartNew(category, counterName);

                if (recorder.Valid)
                {
                    recordersByCounterName[counterName] = recorder;
                }
                else
                {
                    recorder.Dispose();
                }
            }

            Log.Info($"Debug panel watches {recordersByCounterName.Count} of {ProfilerCounters.Length} profiler counters");
        }

        private void TogglePanel()
        {
            visible = !visible;
            panelLabel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            secondsToPanelRefresh = 0;
        }

        private void SampleFrameDuration()
        {
            frameDurations[nextFrameIndex] = Time.unscaledDeltaTime;
            nextFrameIndex = (nextFrameIndex + 1) % FrameSamples;
            if (sampledFrameCount < FrameSamples) sampledFrameCount++;
        }

        private string BuildPanelText()
        {
            panelTextBuilder.Clear();

            AppendFrameLine();
            AppendPanelLine($"Batches {FormatCounterCount("Batches Count")}", $"Triangles {FormatCounterCount("Triangles Count")}");
            AppendPanelLine($"Vertices {FormatCounterCount("Vertices Count")}");

            AppendBlankLine();
            AppendPanelLine($"VRAM {FormatVideoMemory()}", SystemInfo.graphicsDeviceName);
            AppendPanelLine($"RAM {FormatCounterMegabytes("System Used Memory")} / {SystemInfo.systemMemorySize} MB", $"{SystemInfo.processorType.Trim()} ({SystemInfo.processorCount})");
            AppendPanelLine($"GC {FormatManagedMemory()}", $"Reserved {FormatCounterMegabytes("Total Reserved Memory")}");

            AppendBlankLine();
            AppendPanelLine($"Screen {Screen.width}×{Screen.height}", Invariant($"{Screen.currentResolution.refreshRateRatio.value:F0} Hz   ") + (Screen.fullScreen ? "fullscreen" : "windowed"));
            AppendCameraLine();
            AppendPanelLine($"Version {Application.version}");

            AppendBehaviourLines();

            return panelTextBuilder.ToString().TrimEnd();
        }

        private void AppendFrameLine()
        {
            if (sampledFrameCount == 0) return;

            float totalDuration = 0, worstDuration = 0;
            for (int i = 0; i < sampledFrameCount; i++)
            {
                totalDuration += frameDurations[i];
                if (frameDurations[i] > worstDuration) worstDuration = frameDurations[i];
            }

            float averageDuration = totalDuration / sampledFrameCount;
            AppendPanelLine(Invariant($"FPS {1f / averageDuration:F0}"), Invariant($"Frame {averageDuration * 1000f:F1} ms   Worst {worstDuration * 1000f:F1} ms"));
        }

        private void AppendCameraLine()
        {
            Camera camera = Camera.main;
            if (camera == null) return;

            Transform cameraTransform = camera.transform;
            Vector3 position = cameraTransform.position;
            AppendPanelLine(Invariant($"Camera {position.x:F1} {position.y:F1} {position.z:F1}"), FormatFacing(cameraTransform.eulerAngles.y));
        }

        private void AppendBehaviourLines()
        {
            Registers registers = Registers.Instance;

            AppendBlankLine();
            AppendPanelLine(
                $"Behaviours {FormatCount(registers.GetAll<IShooterBehaviour>(RegisterInactivePolicy.Include).Count)}",
                $"Enabled {FormatCount(registers.GetAll<IShooterBehaviour>(RegisterInactivePolicy.Exclude).Count)}");

            registers.FindLargestClasses(largestClasses, largestClassCounts);
            for (int i = 0; i < LargestClassCount && largestClasses[i] != null; i++)
            {
                AppendPanelLine($"  {largestClasses[i].Name}", FormatCount(largestClassCounts[i]));
            }

            AppendPanelLine(
                $"ShooterUpdate {FormatCount(registers.GetAll<IShooterUpdateable>(RegisterInactivePolicy.Exclude).Count)}/frame",
                $"ShooterLateUpdate {FormatCount(registers.GetAll<IShooterLateUpdateable>(RegisterInactivePolicy.Exclude).Count)}/frame");
            AppendPanelLine($"Registry keys {FormatCount(registers.KeyCount)}", $"Slots allocated {FormatCount(registers.Capacity)}");
        }

        private string FormatManagedMemory()
        {
            return TryGetCounterValue("GC Used Memory", out long bytes)
                ? Invariant($"{bytes / Megabyte:F0} MB")
                : Invariant($"{GC.GetTotalMemory(false) / Megabyte:F0} MB");
        }

        private string FormatFrameRate()
        {
            if (sampledFrameCount == 0) return Unavailable;

            float totalDuration = 0;
            for (int i = 0; i < sampledFrameCount; i++)
            {
                totalDuration += frameDurations[i];
            }

            return Invariant($"{sampledFrameCount / totalDuration:F0}/s");
        }

        private string FormatVideoMemory()
        {
            return $"{FormatCounterMegabytes("Gfx Used Memory")} / {SystemInfo.graphicsMemorySize} MB";
        }

        private static string FormatFacing(float yaw)
        {
            return Invariant($"{CompassDirections[Mathf.RoundToInt(yaw / 45f) & 7]} ({yaw:F0}°)");
        }

        private string FormatCounterMegabytes(string counterName)
        {
            return TryGetCounterValue(counterName, out long bytes) ? Invariant($"{bytes / Megabyte:F0} MB") : Unavailable;
        }

        private string FormatCounterCount(string counterName)
        {
            return TryGetCounterValue(counterName, out long count) ? FormatCount(count) : Unavailable;
        }

        private static string FormatCount(long count)
        {
            return count.ToString("N0", CultureInfo.InvariantCulture);
        }

        private bool TryGetCounterValue(string counterName, out long value)
        {
            value = 0;

            if (!recordersByCounterName.TryGetValue(counterName, out ProfilerRecorder recorder) || !recorder.Valid) return false;

            value = recorder.LastValue;

            return true;
        }

        private void AppendPanelLine(string left, string right = null)
        {
            panelTextBuilder.Append(string.IsNullOrEmpty(right) ? left : left.PadRight(LeftColumnWidth) + right).Append('\n');
        }

        private void AppendBlankLine()
        {
            panelTextBuilder.Append('\n');
        }
    }
}
