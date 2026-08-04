using Shooter.Game.Llm.Ticker.Children;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm.Ticker
{
    [RequireComponent(typeof(Llm))]
    public class LlmMainTicker : MonoBehaviour
    {
        private Journal log = Logs.Here();

        private Llm llm;
        private LlmChildTicker[] tickers;

        public void Awake()
        {
            llm = GetComponent<Llm>();
            tickers = GetComponents<LlmChildTicker>();
            if (tickers.Length == 0)
            {
                log.Warn("Entity {} does not have any ticker!", name);
            }
        }

        private bool TickRequired()
        {
            LlmStatus llmStatus = llm.Status();
            foreach (LlmChildTicker ticker in tickers)
            {
                if (ticker.TickRequired(llmStatus))
                {
                    return true;
                }
            }
            return false;
        }

        private void RegisterTick()
        {
            foreach (LlmChildTicker ticker in tickers)
            {
                ticker.RegisterTick();
            }
        }

        public void Update()
        {
            if (TickRequired())
            {
                RegisterTick();
                _ = llm.Tick();
            }
        }
    }
}
