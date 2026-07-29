using System;
using System.IO;
using Newtonsoft.Json;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Configuring
{
    public static class Config
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        private static GameConfig current;

        public static GameConfig Read()
        {
            if (current != null) return current;

            current = Load();
            return current;
        }

        public static void Save()
        {
            if (current == null) return;

            Write(Location(), current);
        }

        private static GameConfig Load()
        {
            string path = Location();

            if (!File.Exists(path))
            {
                var fresh = new GameConfig();
                Write(path, fresh);
                Log.Info("Config {} was absent, wrote defaults", path);
                return fresh;
            }

            try
            {
                var config = JsonConvert.DeserializeObject<GameConfig>(File.ReadAllText(path), Settings)
                             ?? new GameConfig();
                Log.Info("Config {} read", path);
                Write(path, config);
                return config;
            }
            catch (Exception e)
            {
                Log.Error("Config {} is unreadable, falling back to defaults: {}", path, e.Message);
                return new GameConfig();
            }
        }

        private static void Write(string path, object config)
        {
            try
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(config, Settings));
            }
            catch (Exception e)
            {
                Log.Error("Config {} can not be written: {}", path, e.Message);
            }
        }

        private static string Location()
        {
            string folder = Application.isEditor
                ? Path.GetFullPath(Path.Combine(Application.dataPath, ".."))
                : Application.persistentDataPath;

            return Path.Combine(folder, GameConfig.FileName);
        }
    }
}
