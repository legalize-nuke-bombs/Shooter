using System;
using System.Collections.Generic;
using System.Text;
using Shooter.Game.Llm;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;
using Environment = Shooter.Game.World.Environment;

namespace Shooter.Game.Body.Intoxication
{
    public class Intoxication : NetworkBehaviour, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        private ToxinCatalog toxins;
        private readonly NetworkList<double> levels = new NetworkList<double>();
        private readonly Dictionary<string, int> indexes = new Dictionary<string, int>();

        private void Awake()
        {
            toxins = Environment.Current.Toxins;
            for (int i = 0; i < toxins.Count; i++)
            {
                string toxinName = toxins.At(i).name;
                indexes.Add(toxinName, i);
                levels.Add(0);
            }
            Log.Info($"Entity {name} locally registered {levels.Count} - {indexes.Count} toxins");
        }

        public void Intoxicate(ToxinSpec toxin, double amount)
        {
            int index = indexes[toxin.name];
            amount = Math.Max(0, amount);
            levels[index] = Math.Min(100, levels[index] + amount);
        }

        private float timer;
        [SerializeField] private float timerInterval = 0.25f;
        private void Update()
        {
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
            foreach (string toxinName in indexes.Keys)
            {
                int toxinIndex = indexes[toxinName];
                ToxinSpec toxin = toxins.Of(toxinName);
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
            var sb = new StringBuilder();

            foreach (string toxinName in indexes.Keys)
            {
                int toxinIndex = indexes[toxinName];
                if (levels[toxinIndex] < digestThreshold)
                {
                    continue;
                }
                ToxinSpec toxin = toxins.Of(toxinName);
                sb.Append(toxin.PromptName + $" {levels[toxinIndex]} / 100. ");
            }

            return sb.Length == 0
                ? "Sober"
                : "Intoxication effects: " + sb;
        }

        public DigestionPriority Priority => DigestionPriority.Medium;
    }
}
