using UnityEngine;

namespace Shooter.Game.Llm.Ticker.Children
{
    public class LlmIntervalTicker : LlmChildTicker
    {
        [SerializeField] private float interval = 300f;
        private float timer = 5f; // waiting 5s to make sure everything was initializated

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
