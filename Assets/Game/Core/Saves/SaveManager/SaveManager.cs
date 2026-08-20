using System.Collections;
using System.IO;
using Shooter.Configuring;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core.Saves
{
    [RequireComponent(typeof(SnapshotManager))]
    [RequireComponent(typeof(PreviewManager))]
    [RequireComponent(typeof(MainCompressionManager))]
    public class SaveManager : MonoBehaviour
    {
        [SerializeField] private string folder = "Saves";
        [SerializeField] private string prefix = "ShooterSave";
        [SerializeField] private string stampFormat = "yyyy_MM_dd_HH_mm_ss";

        private static readonly Journal Log = Logs.Here();

        private SnapshotManager snapshotManager;
        private PreviewManager previewManager;
        private MainCompressionManager compressionManager;

        public static SaveManager Current { get; private set; }

        private void Awake()
        {
            snapshotManager = GetComponent<SnapshotManager>();
            previewManager = GetComponent<PreviewManager>();
            compressionManager = GetComponent<MainCompressionManager>();

            Current = this;
        }

        public IEnumerator SaveCoroutine()
        {
            Log.Info($"Entity {name} is making save...");
            Snapshot snapshot = snapshotManager.Build();

            string path = Path.Combine(Config.Root(), folder, prefix + "_" + snapshot.Stamp.ToString(stampFormat));

            snapshotManager.Write(Path.Combine(path, "Snapshot.json"), snapshot);
            yield return StartCoroutine(previewManager.WriteCoroutine(Path.Combine(path, "Preview.jpg")));
            path = compressionManager.Compress(path);
            Log.Info($"Entity {name} saved to {path}");
        }

        public void Load()
        {
        }
    }
}
