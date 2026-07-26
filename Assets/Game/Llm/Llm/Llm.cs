using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Shooter.Configuring;
using Shooter.Game.Body;
using Shooter.Logging;

namespace Shooter.Game.Llm
{
    public class Llm : NetworkBehaviour
    {
        private const int MemoryLimit = 2000;

        [SerializeField] [TextArea(4, 12)] private string character;

        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);

        private string memory = "";

        public async Task<string> Ask(string situation, IReadOnlyList<LlmMessage> messages)
        {
            if (gate.CurrentCount == 0)
            {
                Log.Info("Entity {} has an llm request in flight, waiting for a free slot", name);
            }

            await gate.WaitAsync();
            try
            {
                LlmConfig config = Config.Read<ServerConfig>(ServerConfig.FileName).Llm;
                string systemPrompt = LlmPrompt.System(character, memory, WorldState(), situation);

                Log.Info("Entity {} is asking {} for an answer", name, config.Model);
                LlmAnswer answer = await LlmProviders.For(config).Request(config, systemPrompt, messages);
                Remember(answer.Memory);
                return answer.Reply;
            }
            finally
            {
                gate.Release();
            }
        }

        private string WorldState()
        {
            string time = Environment.Current == null ? "неизвестно" : Environment.Current.Clock.DateTime();

            return "Игровое время: " + time + "\n" +
                   "Твоё состояние:\n" + Digestion.Of(this);
        }

        private void Remember(string update)
        {
            if (update == null) return;

            memory = update.Length <= MemoryLimit ? update : update.Substring(0, MemoryLimit);
            Log.Info("Entity {} rewrote its memory ({} chars): {}", name, memory.Length, memory);
        }
    }
}
