using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shooter.Infrastructure;
using Unity.Netcode;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class DebugOverlay : Overlay
    {
        private const string PanelLabelName = "debug";
        private const int FrameSamples = 120;
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
        private int sampledFrameCount;
        private int nextFrameIndex;
        private HostVersion hostVersion;

        private Label panelLabel;
        private float secondsToPanelRefresh;
        private float secondsToLogReport = LogReportSeconds;
        private bool visible;

        private void Start()
        {
            hostVersion = Registers.GetAll<HostVersion>(RegisterInactivePolicy.Include).Single();
        }

        private void Update()
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
            AppendPanelLine($"Партии {FormatCounterCount("Batches Count")}", $"Треугольники {FormatCounterCount("Triangles Count")}");
            AppendPanelLine($"Вершины {FormatCounterCount("Vertices Count")}");

            AppendBlankLine();
            AppendPanelLine($"VRAM {FormatVideoMemory()}", SystemInfo.graphicsDeviceName);
            AppendPanelLine($"RAM {FormatCounterMegabytes("System Used Memory")} / {SystemInfo.systemMemorySize} МБ", $"{SystemInfo.processorType.Trim()} ({SystemInfo.processorCount})");
            AppendPanelLine($"GC {FormatManagedMemory()}", $"Зарезервировано {FormatCounterMegabytes("Total Reserved Memory")}");

            AppendBlankLine();
            AppendPanelLine($"Экран {Screen.width}×{Screen.height}", $"{Screen.currentResolution.refreshRateRatio.value:F0} Гц   " + (Screen.fullScreen ? "Полный экран" : "Окно"));
            AppendWorldLines();

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
            AppendPanelLine($"FPS {1f / averageDuration:F0}", $"Кадр {averageDuration * 1000f:F1} мс   Худший {worstDuration * 1000f:F1} мс");
        }

        private void AppendWorldLines()
        {
            NetworkManager network = NetworkManager.Singleton;

            AppendBlankLine();

            if (network == null || !network.IsListening)
            {
                AppendPanelLine("Сеть не запущена");
                return;
            }

            string role = network.IsHost ? "Хост" : network.IsServer ? "Сервер" : "Клиент";
            string clientCount = network.IsServer ? $"Клиентов {network.ConnectedClientsIds.Count}" : "";
            string roundTripTime = network.IsClient && !network.IsHost
                ? $"Задержка {network.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId)} мс"
                : "";
            AppendPanelLine(role, clientCount + roundTripTime);

            Transform ownPlayerTransform = OwnPlayer.FindComponent<Transform>();
            if (ownPlayerTransform != null)
            {
                Vector3 position = ownPlayerTransform.position;
                AppendPanelLine($"Позиция {position.x:F1} {position.y:F1} {position.z:F1}", FormatFacing(ownPlayerTransform.eulerAngles.y));
            }

            AppendPanelLine(hostVersion == null
                ? $"Клиент {Application.version}"
                : $"Сервер {hostVersion.Version}   Клиент {Application.version}");
        }

        private string FormatManagedMemory()
        {
            return TryGetCounterValue("GC Used Memory", out long bytes)
                ? $"{bytes / Megabyte:F0} МБ"
                : $"{GC.GetTotalMemory(false) / Megabyte:F0} МБ";
        }

        private string FormatFrameRate()
        {
            if (sampledFrameCount == 0) return Unavailable;

            float totalDuration = 0;
            for (int i = 0; i < sampledFrameCount; i++)
            {
                totalDuration += frameDurations[i];
            }

            return $"{sampledFrameCount / totalDuration:F0}/с";
        }

        private string FormatVideoMemory()
        {
            return $"{FormatCounterMegabytes("Gfx Used Memory")} / {SystemInfo.graphicsMemorySize} МБ";
        }

        private static string FormatFacing(float yaw)
        {
            return $"{CompassDirections[Mathf.RoundToInt(yaw / 45f) & 7]} ({yaw:F0}°)";
        }

        private string FormatCounterMegabytes(string counterName)
        {
            return TryGetCounterValue(counterName, out long bytes) ? $"{bytes / Megabyte:F0} МБ" : Unavailable;
        }

        private string FormatCounterCount(string counterName)
        {
            return TryGetCounterValue(counterName, out long count) ? count.ToString("N0") : Unavailable;
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
