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

        private static readonly System.Collections.Generic.Dictionary<Type, object> Known =
            new System.Collections.Generic.Dictionary<Type, object>();

        public static T Read<T>(string fileName) where T : new()
        {
            if (Known.TryGetValue(typeof(T), out object cached)) return (T)cached;

            T config = Load<T>(fileName);
            Known[typeof(T)] = config;
            return config;
        }

        private static T Load<T>(string fileName) where T : new()
        {
            string path = Path.Combine(Folder(), fileName);

            if (!File.Exists(path))
            {
                var fresh = new T();
                Write(path, fresh);
                Log.Info("Config {} was absent, wrote defaults", path);
                return fresh;
            }

            try
            {
                var config = JsonConvert.DeserializeObject<T>(File.ReadAllText(path), Settings) ?? new T();
                Log.Info("Config {} read", path);
                Write(path, config);
                return config;
            }
            catch (Exception e)
            {
                Log.Error("Config {} is unreadable, falling back to defaults: {}", path, e.Message);
                return new T();
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

        private static string Folder()
        {
            return Application.isEditor
                ? Path.GetFullPath(Path.Combine(Application.dataPath, ".."))
                : Application.persistentDataPath;
        }
    }
}
