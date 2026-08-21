using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using Shooter.Game.Core;
using Shooter.Game.Core.Saves;
using Shooter.Game.World;
using Shooter.Logging;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body
{
    public class Intoxication : NetworkBehaviour, IDigestible, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();
        [SerializeField] private float timerInterval = 0.25f;

        [SerializeField] private float lowThreshold = 1f;

        [SerializeField] private float digestThreshold = 10f;
        private readonly Dictionary<FixedString32Bytes, int> indexes = new();
        private readonly NetworkList<double> levels = new();

        public string ComponentKey => "Intoxication";
        private struct SaveData
        {
            public Dictionary<string, double> Levels { get; set; }
        }
        public object SaveObject()
        {
            var sd = new SaveData()
            {
                Levels = new Dictionary<string, double>()
            };
            foreach (FixedString32Bytes toxinId in indexes.Keys)
            {
                double toxinLevel = levels[indexes[toxinId]];
                if (toxinLevel >= lowThreshold)
                {
                    sd.Levels.Add(toxinId.ToString(), toxinLevel);
                }
            }
            return sd;
        }
        public void LoadObject(JToken token)
        {
            SaveData sd = token.To<SaveData>();
            for (int i = 0; i < levels.Count; i++)
            {
                levels[i] = 0;
            }
            foreach (var kvp in sd.Levels)
            {
                if (!indexes.TryGetValue(kvp.Key, out int toxinIndex))
                {
                    Log.Warn($"Entity {name} failed to load toxin {kvp.Key}");
                    continue;
                }
                levels[toxinIndex] = kvp.Value;
            }
        }

        private float timer;

        private ToxinCatalog toxins;

        private void Awake()
        {
            toxins = Catalogs.Of<ToxinCatalog>();
            for (int i = 0; i < toxins.Count; i++)
            {
                indexes.Add(toxins.At(i).Id, i);
            }

            Log.Info($"Entity {name} indexed {indexes.Count} toxins");
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer || levels.Count > 0) return;

            for (int i = 0; i < indexes.Count; i++)
            {
                levels.Add(0);
            }
        }

        private void Update()
        {
            if (!IsServer) return;
            timer += Time.deltaTime * Clock.Current.Scale;
            if (timer >= timerInterval)
            {
                Tick(timer);
                timer = 0;
            }
        }

        public string Digest(DigestionDetail detail)
        {
            if (detail == DigestionDetail.Brief) return null;

            var sb = new StringBuilder();

            foreach (FixedString32Bytes toxinId in indexes.Keys)
            {
                int toxinIndex = indexes[toxinId];
                if (levels[toxinIndex] < digestThreshold) continue;
                ToxinSpec toxin = toxins.Of(toxinId);
                sb.Append(toxin.Id + $" {levels[toxinIndex]:F0} / 100. ");
            }

            return sb.Length == 0
                ? "Sober"
                : "Intoxication effects: " + sb;
        }

        public DigestionPriority Priority => DigestionPriority.Medium;

        public double Level(ToxinSpec toxin)
        {
            return levels[indexes[toxin.Id]];
        }

        public void Intoxicate(ToxinSpec toxin, double amount)
        {
            int index = indexes[toxin.Id];
            amount = Math.Max(0, amount);
            levels[index] = Math.Min(100, levels[index] + amount);
            Log.Info($"Entity {name} got {toxin.name} {amount}, now {levels[index]}");
        }

        private void Tick(float dt)
        {
            foreach (FixedString32Bytes toxinId in indexes.Keys)
            {
                int toxinIndex = indexes[toxinId];
                ToxinSpec toxin = toxins.Of(toxinId);
                if (levels[toxinIndex] < lowThreshold) continue;
                double decayFactor = Math.Pow(0.5, dt / toxin.HalfLife);
                levels[toxinIndex] *= decayFactor;
                if (levels[toxinIndex] < lowThreshold) levels[toxinIndex] = 0;
            }
        }
    }
}
