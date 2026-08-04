using UnityEngine;

namespace Shooter.Game.Llm.Ticker.Children
{
    public class LlmIntervalTicker : LlmChildTicker
    {
        [SerializeField] private float interval = 300f;
        [SerializeField] private float firstTickDelay = 5f;
        private float timer;

        public void Awake()
        {
            timer = firstTickDelay;
        }

        public void Update()
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
