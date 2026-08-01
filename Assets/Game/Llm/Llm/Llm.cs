using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shooter.Configuring;
using Shooter.Game.Body;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Llm
{
    public class Llm : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private int memoryLimit = 20000;

        [SerializeField] private float nearObjectsScanRadius = 250f;

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
                LlmConfig config = Config.Read().Server.Llm;
                string systemPrompt = LlmPrompt.System(
                    character,
                    memory,
                    memoryLimit,
                    WorldState(),
                    situation
                );

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

            string worldState = "Игровое время: " + time + "\n" +
                                "Твоё состояние:\n" + Digestion.Of(this, DigestionDetail.Full) + "\n" +
                                "Объекты рядом с тобой:\n" + DigestNearObjects();

            Log.Info("Entity {} built the world state: {}", name, worldState);

            return worldState;
        }

        private string DigestNearObjects()
        {
            StringBuilder digest = new StringBuilder();

            foreach (Component nearObject in FindNearObjects())
            {
                string seen = Digestion.Seen(nearObject, DigestionDetail.Brief, transform);
                if (seen != null) digest.AppendLine(seen);
            }

            return digest.ToString();
        }

        private HashSet<Component> FindNearObjects()
        {
            HashSet<Component> found = new HashSet<Component>();

            Collider[] hits = Physics.OverlapSphere(transform.position, nearObjectsScanRadius);

            foreach (Collider hit in hits)
            {
                if (!(hit.GetComponentInParent<IDigestible>() is Component owner)) continue;
                if (owner.gameObject == gameObject) continue;

                found.Add(owner);
            }

            return found;
        }

        private void Remember(string update)
        {
            if (update == null) return;

            memory = update.Length <= memoryLimit ? update : update.Substring(0, memoryLimit);
            Log.Info("Entity {} rewrote its memory ({} chars): {}", name, memory.Length, memory);
        }
    }
}
