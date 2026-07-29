using System.Collections.Generic;
using System.Text;
using Shooter.Client.Playing;
using Shooter.Game;
using Shooter.Logging;
using Unity.Netcode;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface.Overlays
{
    public class DebugOverlay : Overlay
    {
        private static readonly Journal Log = Logs.Here();

        private const string DebugElement = "debug";
        private const int FrameSamples = 120;
        private const int Column = 34;
        private const float RefreshSeconds = 0.25f;
        private const float ReportSeconds = 60f;
        private const float Megabyte = 1024f * 1024f;
        private const string Unavailable = "—";

        private static readonly (ProfilerCategory Category, string Name)[] Counters =
        {
            (ProfilerCategory.Memory, "System Used Memory"),
            (ProfilerCategory.Memory, "Total Reserved Memory"),
            (ProfilerCategory.Memory, "GC Used Memory"),
            (ProfilerCategory.Memory, "Gfx Used Memory"),
            (ProfilerCategory.Render, "Batches Count"),
            (ProfilerCategory.Render, "Triangles Count"),
            (ProfilerCategory.Render, "Vertices Count")
        };

        private readonly Dictionary<string, ProfilerRecorder> recorders = new();
        private readonly float[] frames = new float[FrameSamples];
        private readonly StringBuilder builder = new();

        private Label panel;
        private bool visible;
        private int frame;
        private int filled;
        private float refresh;
        private float report = ReportSeconds;

        private void Update()
        {
            if (!Bound) return;

            Sample();

            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame) Toggle();

            report -= Time.unscaledDeltaTime;
            if (report <= 0)
            {
                report = ReportSeconds;
                Log.Info("FPS {}, VRAM {}, RAM {}, batches {}, triangles {}",
                    Frequency(), Video(), Bytes("System Used Memory"),
                    Count("Batches Count"), Count("Triangles Count"));
            }

            if (!visible) return;

            refresh -= Time.unscaledDeltaTime;
            if (refresh > 0) return;

            refresh = RefreshSeconds;
            panel.text = Report();
        }

        protected override bool Bind(VisualElement root)
        {
            panel = root.Q<Label>(DebugElement);

            if (panel == null)
            {
                Log.Error("Overlay document has no {} label, the debug panel stays hidden", DebugElement);
                return false;
            }

            visible = false;
            frame = filled = 0;
            refresh = 0;
            panel.style.display = DisplayStyle.None;
            Listen();

            return true;
        }

        protected override void Unbind()
        {
            foreach (ProfilerRecorder recorder in recorders.Values) recorder.Dispose();

            recorders.Clear();
            panel = null;
        }

        private void Listen()
        {
            foreach ((ProfilerCategory category, string name) in Counters)
            {
                ProfilerRecorder recorder = ProfilerRecorder.StartNew(category, name);

                if (recorder.Valid) recorders[name] = recorder;
                else recorder.Dispose();
            }

            Log.Info("Debug panel watches {} of {} profiler counters", recorders.Count, Counters.Length);
        }

        private void Toggle()
        {
            visible = !visible;
            panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            refresh = 0;
        }

        private void Sample()
        {
            frames[frame] = Time.unscaledDeltaTime;
            frame = (frame + 1) % FrameSamples;
            if (filled < FrameSamples) filled++;
        }

        private string Report()
        {
            builder.Clear();

            Frames();
            Line($"Партии {Count("Batches Count")}", $"Треугольники {Count("Triangles Count")}");
            Line($"Вершины {Count("Vertices Count")}");

            Gap();
            Line($"VRAM {Video()}", SystemInfo.graphicsDeviceName);
            Line($"RAM {Bytes("System Used Memory")} / {SystemInfo.systemMemorySize} МБ",
                $"{SystemInfo.processorType.Trim()} ({SystemInfo.processorCount})");
            Line($"GC {Managed()}", $"Зарезервировано {Bytes("Total Reserved Memory")}");

            Gap();
            Line($"Экран {Screen.width}×{Screen.height}",
                $"{Screen.currentResolution.refreshRateRatio.value:F0} Гц   " +
                (Screen.fullScreen ? "Полный экран" : "Окно"));
            World();

            return builder.ToString().TrimEnd();
        }

        private void Frames()
        {
            if (filled == 0) return;

            float total = 0, worst = 0;
            for (int i = 0; i < filled; i++)
            {
                total += frames[i];
                if (frames[i] > worst) worst = frames[i];
            }

            float average = total / filled;
            Line($"FPS {1f / average:F0}", $"Кадр {average * 1000f:F1} мс   Худший {worst * 1000f:F1} мс");
        }

        private void World()
        {
            NetworkManager network = NetworkManager.Singleton;

            Gap();

            if (network == null || !network.IsListening)
            {
                Line("Сеть не запущена");
                return;
            }

            string role = network.IsHost ? "Хост" : network.IsServer ? "Сервер" : "Клиент";
            string peers = network.IsServer ? $"Клиентов {network.ConnectedClientsIds.Count}" : "";
            string delay = network.IsClient && !network.IsHost
                ? $"Задержка {network.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId)} мс"
                : "";
            Line(role, peers + delay);

            Transform player = OwnPlayer.Find<Transform>();
            if (player != null)
            {
                Vector3 at = player.position;
                Line($"Позиция {at.x:F1} {at.y:F1} {at.z:F1}", Facing(player.eulerAngles.y));
            }

            Environment environment = Environment.Current;
            Line(environment == null ? "Мир не получен" : $"Мир {environment.World}",
                environment == null
                    ? $"Клиент {Application.version}"
                    : $"Сервер {environment.Version}   Клиент {Application.version}");
        }

        private string Managed()
        {
            return Sampled("GC Used Memory", out long value)
                ? $"{value / Megabyte:F0} МБ"
                : $"{System.GC.GetTotalMemory(false) / Megabyte:F0} МБ";
        }

        private string Frequency()
        {
            if (filled == 0) return Unavailable;

            float total = 0;
            for (int i = 0; i < filled; i++) total += frames[i];

            return $"{filled / total:F0}/с";
        }

        private string Video()
        {
            return $"{Bytes("Gfx Used Memory")} / {SystemInfo.graphicsMemorySize} МБ";
        }

        private static string Facing(float yaw)
        {
            string[] sides = { "север", "северо-восток", "восток", "юго-восток", "юг", "юго-запад", "запад", "северо-запад" };

            return $"{sides[Mathf.RoundToInt(yaw / 45f) & 7]} ({yaw:F0}°)";
        }

        private string Bytes(string name)
        {
            return Sampled(name, out long value) ? $"{value / Megabyte:F0} МБ" : Unavailable;
        }

        private string Count(string name)
        {
            return Sampled(name, out long value) ? value.ToString("N0") : Unavailable;
        }

        private bool Sampled(string name, out long value)
        {
            value = 0;

            if (!recorders.TryGetValue(name, out ProfilerRecorder recorder) || !recorder.Valid) return false;

            value = recorder.LastValue;

            return true;
        }

        private void Line(string left, string right = null)
        {
            builder.Append(string.IsNullOrEmpty(right) ? left : left.PadRight(Column) + right).Append('\n');
        }

        private void Gap()
        {
            builder.Append('\n');
        }
    }
}
