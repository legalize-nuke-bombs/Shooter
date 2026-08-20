using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shooter.Configuring;
using Shooter.Game.Core.GameObject;
using Shooter.Game.Core.Screenshots;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core.Saves
{
    public class SaveManager : MonoBehaviour
    {
        [SerializeField] private string folder = "Saves";
        [SerializeField] private string prefix = "ShooterSave";
        [SerializeField] private string stampFormat = "yyyy_MM_dd_HH_mm_ss";
        [SerializeField] private ScreenshotSetting previewSetting;

        private static readonly Journal Log = Logs.Here();

        public static SaveManager Current { get; private set; }

        private static readonly JsonSerializerSettings Settings = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        private class WorldSnapshot
        {
            public string Version { get; set; }
            public string Stamp { get; set; }
            public Dictionary<string, JObject> Entities { get; set; } = new();
        }

        private Register<SaveableObject> saveables;

        private void Awake()
        {
            saveables = Registers.Current.Of<SaveableObject>();
            Current = this;
        }

        private WorldSnapshot BuildSnapshot()
        {
            Log.Info("Building snapshot...");
            var serializer = JsonSerializer.Create(Settings);
            var snapshot = new WorldSnapshot
            {
                Version = Application.version,
                Stamp = DateTime.Now.ToString(stampFormat, CultureInfo.InvariantCulture)
            };

            foreach (SaveableObject saveable in saveables.All)
            {
                if (!saveable.TryGetComponent(out GameObjectId saveableId))
                {
                    Log.Warn($"Entity {saveable.name} does not have id");
                    continue;
                }

                if (string.IsNullOrEmpty(saveableId.Id))
                {
                    Log.Warn($"Entity {saveable.name} id is not set");
                    continue;
                }

                JObject saveableData = null;
                try
                {
                    Dictionary<string, object> saveableDatRaw = saveable.Save();
                    saveableData = JObject.FromObject(saveableDatRaw, serializer);
                }
                catch (Exception e)
                {
                    Log.Warn($"Entity {saveable.name} can not be serialized: {e.Message}");
                }
                if (saveableData == null)
                {
                    continue;
                }

                if (!snapshot.Entities.TryAdd(saveableId.Id, saveableData))
                {
                    Log.Warn($"Entity {saveable.name} shares id {saveableId.Id} with an entity already saved");
                    continue;
                }
            }

            Log.Info("Snapshot built");
            return snapshot;
        }

        private void WriteSnapshot(string path, WorldSnapshot snapshot)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonConvert.SerializeObject(snapshot, Settings));
                Log.Info($"Snapshot saved into {path}: {snapshot.Entities.Count} entities");
            }
            catch (Exception e)
            {
                Log.Error($"Snapshot {path} can not be written: {e.Message}");
            }
        }

        public IEnumerator SaveCoroutine()
        {
            WorldSnapshot snapshot = BuildSnapshot();

            string directory = Path.Combine(Config.Root(), folder, prefix + "_" + snapshot.Stamp);

            WriteSnapshot(Path.Combine(directory, "Snapshot.json"), snapshot);

            yield return StartCoroutine(ScreenshotManager.Current.SaveCoroutine(Path.Combine(directory, "Preview.jpg"), previewSetting));

            Log.Info("Compressing...");
            bool compressed = false;
            try
            {
                ZipFile.CreateFromDirectory(directory, directory + ".zip");
                compressed = true;
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to compress {directory}: {e.Message}");
            }

            if (compressed)
            {
                Log.Info("Compressed successfully, deleting original directory...");
                try
                {
                    Directory.Delete(directory, true);
                    Log.Info("Original directory deleted successfully");
                }
                catch (Exception e)
                {
                    Log.Warn($"Failed to delete original directory: {e.Message}");
                }
            }
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
