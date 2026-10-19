using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace Shooter.Infrastructure
{
    public static class Config
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            Formatting = Formatting.Indented,
            Converters = { new StringEnumConverter() }
        };

        private static GameConfig gameConfig;

        public static GameConfig GetGameConfig()
        {
            if (gameConfig != null) return gameConfig;

            gameConfig = LoadGameConfig();
            return gameConfig;
        }

        public static void SaveGameConfig()
        {
            if (gameConfig == null) return;

            WriteToFile(GetConfigPath(), gameConfig);
        }

        public static Account Account => GetGameConfig().Account;

        public static string GetRootFolder()
        {
            return Application.isEditor ? Path.GetFullPath(Path.Combine(Application.dataPath, "..")) : Application.persistentDataPath;
        }

        private static GameConfig LoadGameConfig()
        {
            string path = GetConfigPath();

            if (!File.Exists(path))
            {
                GameConfig defaultConfig = new();
                WriteToFile(path, defaultConfig);
                Log.Info($"Config {path} was absent, wrote defaults");
                return defaultConfig;
            }

            try
            {
                GameConfig config = JsonConvert.DeserializeObject<GameConfig>(File.ReadAllText(path), JsonSettings) ?? new GameConfig();
                Log.Info($"Config {path} read");
                WriteToFile(path, config);
                return config;
            }
            catch (Exception e)
            {
                Log.Warn($"Config {path} is unreadable, falling back to defaults: {e.Message}");
                return new GameConfig();
            }
        }

        private static void WriteToFile(string path, object config)
        {
            try
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(config, JsonSettings));
            }
            catch (Exception e)
            {
                Log.Warn($"Config {path} can not be written: {e.Message}");
            }
        }

        private static string GetConfigPath()
        {
            return Path.Combine(GetRootFolder(), GameConfig.FileName);
        }
    }
}
