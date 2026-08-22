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

        private readonly SemaphoreSlim gate = new(1, 1);

        private SnapshotManager snapshotManager;
        private MetaManager metaManager;
        private PreviewManager previewManager;

        public static SaveManager Current { get; private set; }

        public bool Saving => gate.CurrentCount == 0;

        private void Awake()
        {
            snapshotManager = GetComponent<SnapshotManager>();
            metaManager = GetComponent<MetaManager>();
            previewManager = GetComponent<PreviewManager>();

            Current = this;
        }

        public IEnumerator SaveCoroutine()
        {
            if (!gate.Wait(0))
            {
                Log.Info($"Entity {name} is already saving, this request is dropped");
                yield break;
            }

            try
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
            finally
            {
                gate.Release();
            }
        }

        public FrozenWorld Freeze()
        {
            Log.Info($"Entity {name} is freezing the world...");
            return FrozenWorld.Freeze();
        }

        public bool Load(FrozenWorld world, string path)
        {
            Log.Info($"Entity {name} is loading from {path}...");

            byte[] snapshot = MainCompressionManager.Current.Read(path, "Snapshot.json");
            if (snapshot == null)
            {
                Log.Error($"Entity {name} found no snapshot in {path}, the world stays frozen");
                return false;
            }

            if (!snapshotManager.Load(world, snapshot)) return false;

            world.Thaw();
            Log.Info($"Entity {name} loaded {path}");
            return true;
        }
    }
}
