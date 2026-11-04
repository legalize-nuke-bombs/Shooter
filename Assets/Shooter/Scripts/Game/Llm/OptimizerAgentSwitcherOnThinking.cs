using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Llm))]
    [RequireComponent(typeof(OptimizerAgent))]
    public class OptimizerAgentSwitcherOnThinking : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Llm llm;
        private OptimizerAgent agent;

        private void Awake()
        {
            llm = GetComponent<Llm>();
            agent = GetComponent<OptimizerAgent>();
            llm.ThinkingChanged += HandleThinkingChanged;
        }

        private void OnDestroy()
        {
            llm.ThinkingChanged -= HandleThinkingChanged;
        }

        private void HandleThinkingChanged()
        {
            ShooterEntity.SetEnabled(agent, llm.IsThinking);
            Log.Info($"Resident {name} {(llm.IsThinking ? "keeps" : "no longer keeps")} the world around it awake while it thinks");
        }
    }
}
