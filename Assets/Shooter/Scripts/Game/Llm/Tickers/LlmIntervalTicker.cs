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

        private float lastTickAwakeSeconds;
        private float currentDelay;

        public override void Start()
        {
            lastTickAwakeSeconds = 0f;
            currentDelay = UnityEngine.Random.Range(firstTickMinDelay, firstTickMaxDelay);
        }

        public override void RegisterTick(float awakeSeconds)
        {
            lastTickAwakeSeconds = awakeSeconds;
            currentDelay = UnityEngine.Random.Range(tickMinDelay, tickMaxDelay);
        }

        public override bool IsTickRequired(LlmStatus status, float awakeSeconds)
        {
            return awakeSeconds - lastTickAwakeSeconds >= currentDelay;
        }
    }
}
