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

        [SerializeField] private string corePrompt =
            "Ты неигровой персонаж (NPC) в 3D мета-хорроре с опциональным кооперативным режимом.\n" +
            "Ты никогда не упоминаешь ничего, что связано с программированием (если это не часть твоего лора).\n" +
            "Ты не обязан ничего игрокам. Игроки — чужаки, и они часто лгут.\n" +
            "ВАЖНО: Атмосфера игры мрачная и пугающая. Твой тон должен быть реалистичным, настороженным или зловещим. Категорически запрещено использовать эмодзи, проявлять излишний энтузиазм или звучать как виртуальный ассистент.";

        [SerializeField] private string character;

        [SerializeField] private int memoryLimit = 20000;
        private string memory = "Пока пусто.";
        private string MemoryPrompt =>
            "У тебя есть постоянная Память, которую ты поддерживаешь.\n" +
            "Чтобы обновить Память, ты в поле memory ответа возвращаешь новую ПОЛНУЮ версию своей Памяти, либо null, если менять нечего.\n" +
            "То, что ты не перенесешь в новую версию памяти, будет безвозвратно утеряно.\n" +
            "Ты хранишь в Памяти максимально подробные сведения об этом мире, о себе.\n" +
            "Ты НЕ хранишь в Памяти подробные личные детали игроков: они живут в переписках с ними.\n" +
            "Ты держишь память короче " + memoryLimit + " символов.\n" +
            "Твоя память сейчас:\n" +
            memory;

        private string Prompt(string situation)
        {
            return new Prompt()
                .Section("Главное", corePrompt)
                .Section("Личность", character)
                .Section("Память", MemoryPrompt)
                .Section("Состояние мира", WorldState())
                .Text(situation)
                .ToString();
        }

        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);

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
                Log.Info("Entity {} is asking {} for an answer", name, config.Model);
                LlmAnswer answer = await LlmProviders.For(config).Request(config, Prompt(situation), messages);
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
            var digest = new StringBuilder();

            foreach (Component nearObject in FindNearObjects())
            {
                string seen = Digestion.Seen(nearObject, DigestionDetail.Brief, transform);
                if (seen != null) digest.AppendLine(seen);
            }

            return digest.ToString();
        }

        [SerializeField] private float nearObjectsScanRadius = 250f;
        private HashSet<Component> FindNearObjects()
        {
            var found = new HashSet<Component>();

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
