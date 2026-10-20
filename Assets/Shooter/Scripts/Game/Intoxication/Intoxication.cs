using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shooter.Infrastructure;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public class Intoxication : NetworkBehaviour, IDigestible, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private float timerInterval = 0.25f;
        [SerializeField] private float lowThreshold = 1f;
        [SerializeField] private float digestThreshold = 10f;

        private readonly Dictionary<FixedString32Bytes, int> levelIndexByToxinId = new();
        private readonly NetworkList<double> levels = new();

        private float secondsSinceDecay;
        private Clock clock;
        private ToxinCatalog toxinCatalog;

        public string ComponentKey => "Intoxication";

        private struct SaveData
        {
            public Dictionary<string, double> Levels { get; set; }
        }

        public object SaveObject()
        {
            SaveData saveData = new()
            {
                Levels = new Dictionary<string, double>()
            };
            foreach (FixedString32Bytes toxinId in levelIndexByToxinId.Keys)
            {
                double toxinLevel = levels[levelIndexByToxinId[toxinId]];
                if (toxinLevel >= lowThreshold)
                {
                    saveData.Levels.Add(toxinId.ToString(), toxinLevel);
                }
            }

            return saveData;
        }

        public void LoadObject(SaveToken content)
        {
            SaveData saveData = content.ToObject<SaveData>();
            for (int i = 0; i < levels.Count; i++)
            {
                levels[i] = 0;
            }

            foreach (KeyValuePair<string, double> savedLevel in saveData.Levels)
            {
                if (!levelIndexByToxinId.TryGetValue(savedLevel.Key, out int toxinIndex))
                {
                    Log.Warn($"Entity {name} failed to load toxin {savedLevel.Key}");
                    continue;
                }

                levels[toxinIndex] = savedLevel.Value;
            }
        }

        private void Awake()
        {
            clock = Registers.Get<Clock>();
            toxinCatalog = Catalogs.GetCatalog<ToxinCatalog>();
            for (int i = 0; i < toxinCatalog.Count; i++)
            {
                levelIndexByToxinId.Add(toxinCatalog.GetAssetAt(i).NetworkId, i);
            }

            Log.Info($"Entity {name} indexed {levelIndexByToxinId.Count} toxins");
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer || levels.Count > 0) return;

            for (int i = 0; i < levelIndexByToxinId.Count; i++)
            {
                levels.Add(0);
            }
        }

        private void Update()
        {
            if (!IsServer) return;
            secondsSinceDecay += Time.deltaTime * clock.Scale;
            if (secondsSinceDecay >= timerInterval)
            {
                DecayLevels(secondsSinceDecay);
                secondsSinceDecay = 0;
            }
        }

        public string Digest(DigestionDetail detail)
        {
            if (detail == DigestionDetail.Brief) return null;

            StringBuilder effects = new();

            foreach (FixedString32Bytes toxinId in levelIndexByToxinId.Keys)
            {
                int toxinIndex = levelIndexByToxinId[toxinId];
                if (levels[toxinIndex] < digestThreshold) continue;
                ToxinAsset toxin = toxinCatalog.GetAssetById(toxinId);
                effects.Append(toxin.Id + $" {levels[toxinIndex]:F0} / 100. ");
            }

            return effects.Length == 0 ? "Sober" : "Intoxication effects: " + effects;
        }

        public DigestionPriority Priority => DigestionPriority.Medium;

        public double GetLevel(ToxinAsset toxin)
        {
            return levels[levelIndexByToxinId[toxin.NetworkId]];
        }

        public void AddToxin(ToxinAsset toxin, double amount)
        {
            int toxinIndex = levelIndexByToxinId[toxin.NetworkId];
            amount = Math.Max(0, amount);
            levels[toxinIndex] = Math.Min(100, levels[toxinIndex] + amount);
            Log.Info($"Entity {name} got {toxin.name} {amount}, now {levels[toxinIndex]}");
        }

        private void DecayLevels(float dt)
        {
            foreach (FixedString32Bytes toxinId in levelIndexByToxinId.Keys)
            {
                int toxinIndex = levelIndexByToxinId[toxinId];
                ToxinAsset toxin = toxinCatalog.GetAssetById(toxinId);
                if (levels[toxinIndex] < lowThreshold) continue;
                double decayFactor = Math.Pow(0.5, dt / toxin.HalfLife);
                levels[toxinIndex] *= decayFactor;
                if (levels[toxinIndex] < lowThreshold) levels[toxinIndex] = 0;
            }
        }
    }
}
