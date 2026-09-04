using System;
using UnityEngine;

namespace Shooter.Game.Llm
{
    [Serializable]
    public class LlmIntervalTicker : LlmChildTicker
    {
        [SerializeField] private float interval = 300f;
        [SerializeField] private float firstTickDelay = 60f;

        private float lastTickTime;
        private bool first;

        public override void OnStart()
        {
            lastTickTime = Time.time;
            first = true;
        }

        public override void RegisterTick()
        {
            lastTickTime = Time.time;
            first = false;
        }

        public override bool TickRequired(LlmStatus llmStatus)
        {
            if (first)
            {
                return Time.time - lastTickTime >= firstTickDelay;
            }
            return Time.time - lastTickTime >= interval;
        }
    }
}
