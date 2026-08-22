using System.Collections;
using System.IO;
using System.Threading;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core.Saves
{
    [RequireComponent(typeof(SnapshotManager))]
    [RequireComponent(typeof(MetaManager))]
    [RequireComponent(typeof(PreviewManager))]
    public class SaveManager : MonoBehaviour
    {
        [SerializeField] private string prefix = "ShooterSave";
        [SerializeField] private string stampFormat = "yyyy_MM_dd_HH_mm_ss";

        private static readonly Journal Log = Logs.Here();

        private SnapshotManager snapshotManager;
        private MetaManager metaManager;
        private PreviewManager previewManager;

        public static SaveManager Current { get; private set; }

        private void Awake()
        {
            snapshotManager = GetComponent<SnapshotManager>();
            metaManager = GetComponent<MetaManager>();
            previewManager = GetComponent<PreviewManager>();

            Current = this;
        }

        public IEnumerator SaveCoroutine()
        {
            Log.Info($"Entity {name} is making save...");
            Snapshot snapshot = snapshotManager.Build();
            Meta meta = metaManager.Build();

            string path = Path.Combine(SaveLibrary.Location, prefix + "_" + meta.Stamp.ToString(stampFormat));

            snapshotManager.Write(Path.Combine(path, "Snapshot.json"), snapshot);
            metaManager.Write(Path.Combine(path, "Meta.json"), meta);
            yield return StartCoroutine(previewManager.WriteCoroutine(Path.Combine(path, "Preview.jpg")));
            path = MainCompressionManager.Current.Compress(path);
            Log.Info($"Entity {name} saved to {path}");
        }

        public void Load(string path)
        {
            Log.Info($"Entity {name} is loading from {path}...");

            byte[] snapshot = MainCompressionManager.Current.Read(path, "Snapshot.json");
        }
    }
}
