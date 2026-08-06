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

        private void Awake()
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
            TicksByChildren.TryAdd(type, 0);
            TicksByChildren[type]++;
            ticksTotal++;

            foreach (LlmChildTicker ticker in tickers)
            {
                ticker.RegisterTick();
            }
        }

        private void Update()
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

        private static readonly Dictionary<Type, long> TicksByChildren = new Dictionary<Type, long>();
        private static long ticksTotal;
        private static float nextLogAt;
        [SerializeField] private float loggingInterval = 30f;

        private void HandleLogging()
        {
            if (Time.time < nextLogAt) return;

            nextLogAt = Time.time + loggingInterval;
            LogStatistics();
        }

        private static void LogStatistics()
        {
            var sb = new StringBuilder();
            sb.Append("Llm tick totals: ").Append(ticksTotal);

            if (ticksTotal > 0)
            {
                sb.Append(" (");
                bool first = true;
                foreach (var kvp in TicksByChildren)
                {
                    if (!first) sb.Append(", ");
                    sb.Append(kvp.Key.Name).Append(' ').Append(kvp.Value);
                    first = false;
                }
                sb.Append(')');
            }

            Log.Info(sb.ToString());
        }
    }
}
