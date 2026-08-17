using System;
using Shooter.Game.Core;
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
        private LlmChildTicker[] tickers;

        private void Awake()
        {
            llm = GetComponent<Llm>();

            netObject = GetComponentInParent<NetworkObject>();
            if (netObject == null) Log.Warn($"Entity {entityName} has no NetworkObject, its llm will never tick");

            entityName = this.NameOf();

            tickers = GetComponents<LlmChildTicker>();
            if (tickers.Length == 0) Log.Warn($"Entity {entityName} does not have any ticker!");
        }

        private void Update()
        {
            if (netObject == null || !netObject.IsSpawned || !netObject.NetworkManager.IsServer) return;

            Type type = TickRequired();
            if (type != null) Tick(type);
        }

        private Type TickRequired()
        {
            LlmStatus llmStatus = llm.Status();
            foreach (LlmChildTicker ticker in tickers)
                if (ticker.TickRequired(llmStatus))
                    return ticker.GetType();

            return null;
        }

        private void RegisterTick(Type type)
        {
            foreach (LlmChildTicker ticker in tickers) ticker.RegisterTick();
        }

        private async void Tick(Type type)
        {
            try
            {
                bool success = await llm.Tick();

                if (success) RegisterTick(type);
            }
            catch (Exception ex)
            {
                Log.Error($"Entity {entityName} failed to execute LLM Tick: {ex.Message}");
            }
        }
    }
}
