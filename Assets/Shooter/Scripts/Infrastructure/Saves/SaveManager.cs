using System.Collections;
using System.IO;

namespace Shooter.Infrastructure
{
    public static class SaveManager
    {
        private const string Prefix = "ShooterSave";
        private const string StampFormat = "yyyy_MM_dd_HH_mm_ss_fff";
        private static readonly Journal Log = Logs.Here();

        public static bool Saving { get; private set; }

        // The gate outlives the world: a save cut short by its runner's death never reaches the finally below
        public static void Open()
        {
            if (!Saving) return;

            Saving = false;
            Log.Warn("The previous world left a save unfinished, the gate is open again");
        }

        public static IEnumerator SaveCoroutine()
        {
            if (Saving)
            {
                Log.Info("A save is already running, this request is dropped");
                yield break;
            }

            Saving = true;
            try
            {
                Log.Info("Making save...");
                Snapshot snapshot = SnapshotManager.Build();
                Meta meta = MetaManager.Build();

                string path = Path.Combine(SaveLibrary.Location, Prefix + "_" + meta.Stamp.ToString(StampFormat));

                if (!SnapshotManager.Write(Path.Combine(path, "Snapshot.json"), snapshot))
                {
                    Log.Warn($"The save has no snapshot and is abandoned, removing {path}...");
                    MainCompressionManager.Delete(path);
                    yield break;
                }

                MetaManager.Write(Path.Combine(path, "Meta.json"), meta);
                yield return PreviewManager.WriteCoroutine(Path.Combine(path, "Preview.jpg"));
                path = MainCompressionManager.Compress(path);
                Log.Info($"Saved to {path}");
            }
            finally
            {
                Saving = false;
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
