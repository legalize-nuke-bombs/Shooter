using System;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Llm))]
    public class LlmMainTicker : MonoBehaviour, IShooterUpdateable
    {
        private static readonly Journal Log = Logs.Here();
        private string entityName;

        private Llm llm;
        [SerializeReference, SubclassSelector] private LlmChildTicker[] tickers;

        // Counts only while the resident is awake: one woken for a moment by someone else's tick isn't overdue, so it
        // doesn't tick in turn and wake its own neighbours across the whole map
        private float awakeSeconds;

        private void Awake()
        {
            ShooterEntity.SetEnabled(this, NetworkManager.Singleton.IsServer);
            entityName = name;

            llm = GetComponent<Llm>();

            foreach (LlmChildTicker ticker in tickers)
            {
                ticker.Start();
            }
        }

        public void ShooterUpdate()
        {
            awakeSeconds += Time.deltaTime;
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
                if (ticker.IsTickRequired(llmStatus, awakeSeconds))
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
                ticker.RegisterTick(awakeSeconds);
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
