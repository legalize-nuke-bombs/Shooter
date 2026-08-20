using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using Shooter.Configuring;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core.Saves
{
    [RequireComponent(typeof(SnapshotManager))]
    [RequireComponent(typeof(PreviewManager))]
    [RequireComponent(typeof(CompressionManager))]
    public class SaveManager : MonoBehaviour
    {
        [SerializeField] private string folder = "Saves";
        [SerializeField] private string prefix = "ShooterSave";
        [SerializeField] private string stampFormat = "yyyy_MM_dd_HH_mm_ss";

        private static readonly Journal Log = Logs.Here();

        private SnapshotManager snapshotManager;
        private PreviewManager previewManager;
        private CompressionManager compressionManager;

        public static SaveManager Current { get; private set; }

        private void Awake()
        {
            snapshotManager = GetComponent<SnapshotManager>();
            previewManager = GetComponent<PreviewManager>();
            compressionManager = GetComponent<CompressionManager>();
            Current = this;
        }

        public IEnumerator SaveCoroutine()
        {
            Log.Info("Saving...");
            Snapshot snapshot = snapshotManager.Build();

            string directory = Path.Combine(Config.Root(), folder, prefix + "_" + snapshot.Stamp.ToString(stampFormat));

            snapshotManager.Write(Path.Combine(directory, "Snapshot.json"), snapshot);
            yield return StartCoroutine(previewManager.WriteCoroutine(Path.Combine(directory, "Preview.jpg")));
            compressionManager.Compress(directory);
            Log.Info($"Saved as {directory + compressionManager.Extension}");
        }

        public void Load()
        {
        }

        // TODO Test
        private float timer = 0;
        private float timerInterval = 5;
        public void Update()
        {
           timer += Time.deltaTime;
           if (timer >= timerInterval)
           {
               StartCoroutine(SaveCoroutine());
               enabled = false;
           }
        }
    }
}
