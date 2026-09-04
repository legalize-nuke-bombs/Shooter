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
        private NetworkObject netObject;
        [SerializeReference, SubclassSelector] private LlmChildTicker[] tickers;

        private void Awake()
        {
            entityName = name;

            llm = GetComponent<Llm>();

            foreach (LlmChildTicker ticker in tickers)
            {
                ticker.OnStart();
            }
        }

        private void Update()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
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

        private void RegisterTick()
        {
            foreach (LlmChildTicker ticker in tickers) ticker.RegisterTick();
        }

        private async void Tick(Type req)
        {
            Log.Info($"Entity {name} will tick, ticker {req}");
            try
            {
                bool success = await llm.Tick();
                if (success) RegisterTick();
            }
            catch (Exception ex)
            {
                Log.Error($"Entity {entityName} failed to execute LLM Tick: {ex.Message}");
            }
        }
    }
}
