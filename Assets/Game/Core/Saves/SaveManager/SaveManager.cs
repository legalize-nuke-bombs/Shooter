using System.Collections;
using System.IO;
using System.Threading;
using Shooter.Logging;

namespace Shooter.Game.Core.Saves
{
    public static class SaveManager
    {
        private const string Prefix = "ShooterSave";
        private const string StampFormat = "yyyy_MM_dd_HH_mm_ss";
        private static readonly Journal Log = Logs.Here();

        private static readonly SemaphoreSlim Gate = new(1, 1);

        public static bool Saving => Gate.CurrentCount == 0;

        // The gate outlives the world: a save cut short by its runner's death never reaches the finally below
        public static void Open()
        {
            if (!Saving) return;

            Gate.Release();
            Log.Warn("The previous world left a save unfinished, the gate is open again");
        }

        public static IEnumerator SaveCoroutine()
        {
            if (!Gate.Wait(0))
            {
                Log.Info("A save is already running, this request is dropped");
                yield break;
            }

            try
            {
                Log.Info("Making save...");
                Snapshot snapshot = SnapshotManager.Build();
                Meta meta = MetaManager.Build();

                string path = Path.Combine(SaveLibrary.Location, Prefix + "_" + meta.Stamp.ToString(StampFormat));

                SnapshotManager.Write(Path.Combine(path, "Snapshot.json"), snapshot);
                MetaManager.Write(Path.Combine(path, "Meta.json"), meta);
                yield return PreviewManager.WriteCoroutine(Path.Combine(path, "Preview.jpg"));
                path = MainCompressionManager.Compress(path);
                Log.Info($"Saved to {path}");
            }
            finally
            {
                Gate.Release();
            }
        }

        public static FrozenWorld Freeze()
        {
            Log.Info("Freezing the world...");
            return FrozenWorld.Freeze();
        }

        public static bool Load(FrozenWorld world, string path)
        {
            Log.Info($"Loading from {path}...");

            byte[] snapshot = MainCompressionManager.Read(path, "Snapshot.json");
            if (snapshot == null)
            {
                Log.Warn($"No snapshot in {path}, the world stays frozen");
                return false;
            }

            if (!SnapshotManager.Load(world, snapshot)) return false;

            world.Thaw();
            Log.Info($"Loaded {path}");
            return true;
        }
    }
}
