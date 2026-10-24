using System;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Llm))]
    public class LlmMainTicker : ShooterBehaviour
    {
        private static readonly Journal Log = Logs.Here();
        private string entityName;

        private Llm llm;
        [SerializeReference, SubclassSelector] private LlmChildTicker[] tickers;

        protected override void ShooterAwake()
        {
            enabled = NetworkManager.Singleton.IsServer;
            entityName = name;

            llm = GetComponent<Llm>();

            foreach (LlmChildTicker ticker in tickers)
            {
                ticker.Start();
            }
        }

        private void Update()
        {
            Type tickReason = FindTickReason();
            if (tickReason != null)
            {
                TryTickLlm(tickReason);
            }
        }

        private Type FindTickReason()
        {
            LlmStatus llmStatus = llm.GetStatus();
            foreach (LlmChildTicker ticker in tickers)
            {
                if (ticker.IsTickRequired(llmStatus))
                {
                    return ticker.GetType();
                }
            }

            return null;
        }

        private void RegisterTick(Type tickReason)
        {
            Log.Info($"Entity {entityName} ticked, reason {tickReason}");
            foreach (LlmChildTicker ticker in tickers)
            {
                ticker.RegisterTick();
            }
        }

        private async void TryTickLlm(Type tickReason)
        {
            try
            {
                bool ticked = await llm.TryTick();
                if (ticked) RegisterTick(tickReason);
            }
            catch (Exception e)
            {
                Log.Error($"Entity {entityName} failed to execute LLM Tick: {e.Message}");
            }
        }
    }
}
