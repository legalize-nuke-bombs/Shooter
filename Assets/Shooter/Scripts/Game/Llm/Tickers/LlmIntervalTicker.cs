using System;
using UnityEngine;

namespace Shooter.Game
{
    [Serializable]
    public class LlmIntervalTicker : LlmChildTicker
    {
        [SerializeField] private float interval = 300f;
        [SerializeField] private float firstTickDelay = 60f;

        private float lastTickTime;
        private bool isWaitingForFirstTick;

        public override void StartTicking()
        {
            lastTickTime = Time.time;
            isWaitingForFirstTick = true;
        }

        public override void RegisterTick()
        {
            lastTickTime = Time.time;
            isWaitingForFirstTick = false;
        }

        public override bool IsTickRequired(LlmStatus status)
        {
            if (isWaitingForFirstTick)
            {
                return Time.time - lastTickTime >= firstTickDelay;
            }
            return Time.time - lastTickTime >= interval;
        }
    }
}
