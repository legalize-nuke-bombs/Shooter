using System;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(Llm))]
    public class LlmMainTicker : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();
        private string entityName;

        private Llm llm;
        [SerializeReference, SubclassSelector] private LlmChildTicker[] tickers;

        private void Awake()
        {
            enabled = NetworkManager.Singleton.IsServer;
            entityName = name;

            llm = GetComponent<Llm>();

            foreach (LlmChildTicker ticker in tickers)
            {
                ticker.OnStart();
            }
        }

        private void Update()
        {
            Type req = TickRequired();
            if (req != null)
            {
                Tick(req);
            }
        }

        private Type TickRequired()
        {
            LlmStatus llmStatus = llm.Status();
            foreach (LlmChildTicker ticker in tickers)
            {
                if (ticker.TickRequired(llmStatus))
                {
                    return ticker.GetType();
                }
            }

            return null;
        }

        private void RegisterTick(Type req)
        {
            Log.Info($"Entity {entityName} ticked, reason {req}");
            foreach (LlmChildTicker ticker in tickers) ticker.RegisterTick();
        }

        private async void Tick(Type req)
        {
            try
            {
                bool success = await llm.Tick();
                if (success) RegisterTick(req);
            }
            catch (Exception ex)
            {
                Log.Error($"Entity {entityName} failed to execute LLM Tick: {ex.Message}");
            }
        }
    }
}
