using System;
using System.Collections.Generic;
using System.Text;
using Shooter.Game.Llm.Ticker.Children;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Llm.Ticker
{
    [RequireComponent(typeof(Llm))]
    public class LlmMainTicker : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Llm llm;
        private NetworkObject netObject;
        private string entityName;
        private LlmChildTicker[] tickers;

        public void Awake()
        {
            llm = GetComponent<Llm>();
            netObject = GetComponent<NetworkObject>();
            entityName = name;
            if (netObject == null)
            {
                Log.Warn("Entity {} has no NetworkObject, its llm will never tick", entityName);
            }
            tickers = GetComponents<LlmChildTicker>();
            if (tickers.Length == 0)
            {
                Log.Warn("Entity {} does not have any ticker!", entityName);
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

        private void RegisterTick(Type type)
        {
            ticksByChildren.TryAdd(type, 0);
            ticksByChildren[type]++;

            foreach (LlmChildTicker ticker in tickers)
            {
                ticker.RegisterTick();
            }
        }

        public void Update()
        {
            if (netObject == null || !netObject.IsSpawned || !netObject.NetworkManager.IsServer)
            {
                return;
            }

            Type type = TickRequired();
            if (type != null)
            {
                Tick(type);
            }
            HandleLogging();
        }

        private async void Tick(Type type)
        {
            try
            {
                bool success = await llm.Tick();

                if (success)
                {
                    RegisterTick(type);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Entity {} failed to execute LLM Tick: {}", entityName, ex.Message);
            }
        }

        private readonly Dictionary<Type, long> ticksByChildren = new Dictionary<Type, long>();
        [SerializeField] private float loggingInterval = 30f;
        private float loggingTimer = 0;
        private void HandleLogging()
        {
            loggingTimer -= Time.deltaTime;
            if (loggingTimer <= 0f)
            {
                LogStatistics();
                loggingTimer = loggingInterval;
            }
        }

        private void LogStatistics()
        {
            var sb = new StringBuilder();
            sb.Append($"{name} stats: ");

            foreach (var kvp in ticksByChildren)
            {
                sb.Append($"{kvp.Key.Name}: {kvp.Value} ticks, ");
            }

            Log.Info(sb.ToString());
        }
    }
}
