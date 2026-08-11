using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(LlmWaiting))]
    public class LlmTalkTicker : LlmChildTicker
    {
        [SerializeField] private float retryInterval = 2f;

        private LlmWaiting waiting;
        private float timer;

        private void Awake()
        {
            waiting = GetComponent<LlmWaiting>();
        }

        private void Update()
        {
            timer -= Time.deltaTime;
        }

        public override void RegisterTick()
        {
            if (waiting.Any) timer = retryInterval;
        }

        public override bool TickRequired(LlmStatus llmStatus)
        {
            return waiting.Any && timer <= 0f;
        }
    }
}
