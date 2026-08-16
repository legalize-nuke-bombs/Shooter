using System;
using System.Collections.Generic;
using System.Text;
using Shooter.Game.Llm;
using Shooter.Logging;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Environment = Shooter.Game.World.Environment;

namespace Shooter.Game.Body
{
    public class Intoxication : NetworkBehaviour, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        private ToxinCatalog toxins;
        private readonly NetworkList<double> levels = new NetworkList<double>();
        private readonly Dictionary<FixedString32Bytes, int> indexes = new Dictionary<FixedString32Bytes, int>();

        public double Level(ToxinSpec toxin)
        {
            return levels[indexes[toxin.Id]];
        }

        private void Awake()
        {
            toxins = Environment.Current.Toxins;
            for (int i = 0; i < toxins.Count; i++)
            {
                FixedString32Bytes toxinId = toxins.At(i).Id;
                indexes.Add(toxinId, i);
                levels.Add(0);
            }
            Log.Info($"Entity {name} locally registered {levels.Count} - {indexes.Count} toxins");
        }

        public void Intoxicate(ToxinSpec toxin, double amount)
        {
            int index = indexes[toxin.Id];
            amount = Math.Max(0, amount);
            levels[index] = Math.Min(100, levels[index] + amount);
            Log.Info($"Entity {name} got {toxin.name} {amount}, now {levels[index]}");
        }

        private float timer;
        [SerializeField] private float timerInterval = 0.25f;
        private void Update()
        {
            if (!IsServer) return;
            timer += Time.deltaTime;
            if (timer >= timerInterval)
            {
                Tick(timer);
                timer = 0;
            }
        }

        [SerializeField] private float lowThreshold = 1f;
        private void Tick(float dt)
        {
            foreach (FixedString32Bytes toxinId in indexes.Keys)
            {
                int toxinIndex = indexes[toxinId];
                ToxinSpec toxin = toxins.Of(toxinId);
                if (levels[toxinIndex] < lowThreshold)
                {
                    continue;
                }
                double decayFactor = Math.Pow(0.5, dt / toxin.HalfLife);
                levels[toxinIndex] *= decayFactor;
                if (levels[toxinIndex] < lowThreshold)
                {
                    levels[toxinIndex] = 0;
                }
            }
        }

        [SerializeField] private float digestThreshold = 10f;
        public string Digest(DigestionDetail detail)
        {
            if (detail == DigestionDetail.Brief)
            {
                return null;
            }

            var sb = new StringBuilder();

            foreach (FixedString32Bytes toxinId in indexes.Keys)
            {
                int toxinIndex = indexes[toxinId];
                if (levels[toxinIndex] < digestThreshold)
                {
                    continue;
                }
                ToxinSpec toxin = toxins.Of(toxinId);
                sb.Append(toxin.Id + $" {levels[toxinIndex]:F0} / 100. ");
            }

            return sb.Length == 0
                ? "Sober"
                : "Intoxication effects: " + sb;
        }

        public DigestionPriority Priority => DigestionPriority.Medium;
    }
}
