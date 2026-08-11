using System;
using System.Collections.Generic;
using Shooter.Game.World;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;
using Environment = Shooter.Game.World.Environment;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(Llm))]
    public class LlmMainTicker : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Llm llm;
        private NetworkObject netObject;
        private string entityName;
        private LlmChildTicker[] tickers;
        private LlmTickProfiler profiler;

        [SerializeField] private float budgetWindow = 20f;
        [SerializeField] private int budgetTicks = 5;

        private readonly Queue<float> recent = new Queue<float>();
        private bool muted;

        private void Awake()
        {
            llm = GetComponent<Llm>();

            netObject = GetComponent<NetworkObject>();
            if (netObject == null)
            {
                Log.Warn($"Entity {entityName} has no NetworkObject, its llm will never tick");
            }

            entityName = name;

            tickers = GetComponents<LlmChildTicker>();
            if (tickers.Length == 0)
            {
                Log.Warn($"Entity {entityName} does not have any ticker!");
            }

            profiler = Environment.Current.Profiler?.Of<LlmTickProfiler>();
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

        private bool Affordable()
        {
            while (recent.Count > 0 && Time.time - recent.Peek() > budgetWindow) recent.Dequeue();

            if (recent.Count < budgetTicks)
            {
                if (muted) Log.Warn($"Entity {entityName} is allowed to think again, {recent.Count} ticks in the last {budgetWindow} s");
                muted = false;

                return true;
            }

            if (!muted) Log.Warn($"Entity {entityName} spent {recent.Count} ticks in {budgetWindow} s and stops thinking until it calms down");
            muted = true;

            return false;
        }

        private void RegisterTick(Type type)
        {
            recent.Enqueue(Time.time);
            profiler?.RegisterTick(type.Name);

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

            if (!Affordable())
            {
                return;
            }

            Type type = TickRequired();
            if (type != null)
            {
                Tick(type);
            }
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
                Log.Error($"Entity {entityName} failed to execute LLM Tick: {ex.Message}");
            }
        }
    }
}
