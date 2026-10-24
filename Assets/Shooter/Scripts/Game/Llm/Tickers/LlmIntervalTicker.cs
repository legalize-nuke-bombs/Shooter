using System;
using UnityEngine;

namespace Shooter.Game
{
    [Serializable]
    public class LlmIntervalTicker : LlmChildTicker
    {
        [SerializeField] private float firstTickMinDelay = 20f;
        [SerializeField] private float firstTickMaxDelay = 120f;
        [SerializeField] private float tickMinDelay = 180f;
        [SerializeField] private float tickMaxDelay = 300f;

        private float lastTickTime;
        private float currentDelay;

        public override void Start()
        {
            lastTickTime = Time.time;
            currentDelay = UnityEngine.Random.Range(firstTickMinDelay, firstTickMaxDelay);
        }

        public override void RegisterTick()
        {
            lastTickTime = Time.time;
            currentDelay = UnityEngine.Random.Range(tickMinDelay, tickMaxDelay);
        }

        public override bool IsTickRequired(LlmStatus status)
        {
            return Time.time - lastTickTime >= currentDelay;
        }
    }
}
