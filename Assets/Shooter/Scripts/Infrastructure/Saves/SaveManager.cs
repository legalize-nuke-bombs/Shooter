using System.Collections;
using System.IO;

namespace Shooter.Infrastructure
{
    public static class SaveManager
    {
        public const string SnapshotFileName = "Snapshot.json";
        public const string MetaFileName = "Meta.json";
        public const string PreviewFileName = "Preview.jpg";
        private const string SaveNamePrefix = "ShooterSave";
        private const string SaveNameStampFormat = "yyyy_MM_dd_HH_mm_ss_fff";
        private static readonly Journal Log = Logs.Here();

        public static bool IsSaving { get; private set; }

        // IsSaving outlives the world: a save cut short by its runner's death never reaches the finally that clears it
        public static void ForgetUnfinishedSave()
        {
            if (!IsSaving) return;

            IsSaving = false;
            Log.Warn("The previous world left a save unfinished, saving is allowed again");
        }

        public static IEnumerator SaveCoroutine()
        {
            if (IsSaving)
            {
                Log.Info("A save is already running, this request is dropped");
                yield break;
            }

            IsSaving = true;
            try
            {
                Log.Info("Making save...");
                Snapshot snapshot = SnapshotManager.BuildSnapshot();
                Meta meta = MetaManager.BuildMeta();

                string saveLocation = Path.Combine(SaveLibrary.FolderPath, SaveNamePrefix + "_" + meta.Stamp.ToString(SaveNameStampFormat));

                if (!SnapshotManager.TryWriteSnapshot(Path.Combine(saveLocation, SnapshotFileName), snapshot))
                {
                    Log.Warn($"The save has no snapshot and is abandoned, removing {saveLocation}...");
                    MainCompressionManager.DeleteArchive(saveLocation);
                    yield break;
                }

                MetaManager.WriteMeta(Path.Combine(saveLocation, MetaFileName), meta);
                yield return PreviewManager.WritePreviewCoroutine(Path.Combine(saveLocation, PreviewFileName));
                saveLocation = MainCompressionManager.CompressFolder(saveLocation);
                Log.Info($"Saved to {saveLocation}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        public static FrozenWorld FreezeWorld()
        {
            Log.Info("Freezing the world...");
            return FrozenWorld.FreezeAllSaveableEntities();
        }

        public static bool LoadSaveAndThawWorld(FrozenWorld frozenWorld, string saveLocation)
        {
            Log.Info($"Loading from {saveLocation}...");

            byte[] snapshotBytes = MainCompressionManager.ReadFile(saveLocation, SnapshotFileName);
            if (snapshotBytes == null)
            {
                Log.Warn($"No snapshot in {saveLocation}, the world stays frozen");
                return false;
            }

            if (!SnapshotManager.TryLoadSnapshot(frozenWorld, snapshotBytes)) return false;

            frozenWorld.ThawAllSaveableEntities();
            Log.Info($"Loaded {saveLocation}");
            return true;
        }
    }
}
