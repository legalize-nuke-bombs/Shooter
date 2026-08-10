using UnityEngine;

namespace Shooter.Game.Llm
{
    public class LlmIntervalTicker : LlmChildTicker
    {
        [SerializeField] private float interval = 300f;
        [SerializeField] private float firstTickDelay = 1f;
        private float timer;

        private void Awake()
        {
            timer = firstTickDelay;
        }

        private void Update()
        {
            timer -= Time.deltaTime;
        }

        public override void RegisterTick()
        {
            timer = interval;
        }

        public override bool TickRequired(LlmStatus llmStatus)
        {
            return timer <= 0f;
        }
    }
}
