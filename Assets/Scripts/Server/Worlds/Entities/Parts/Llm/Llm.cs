using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shooter.Logging;
using Shooter.Server.Protocol;

namespace Shooter.Server.Worlds.Entities.Parts.Llm
{
    public abstract class Llm : Part
    {
        public const int MemoryLimit = 1500;

        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);

        private string memory = "";

        protected Llm(Entity self) : base(self, typeof(Llm))
        {
        }

        public async Task<string> Ask(string systemPrompt, IReadOnlyList<LlmMessage> messages)
        {
            if (gate.CurrentCount == 0)
            {
                Log.Info("Entity {} has an llm request in flight, waiting for a free slot", Self.Name);
            }

            await gate.WaitAsync();
            try
            {
                LlmAnswer answer = await Request(Prompted(systemPrompt), messages);
                Remember(answer.Memory);
                return answer.Reply;
            }
            finally
            {
                gate.Release();
            }
        }

        public sealed override void Apply(PlayerIntent input)
        {
        }

        public sealed override void Tick(float dt)
        {
        }

        public sealed override void Died()
        {
        }

        public sealed override string Digest()
        {
            return null;
        }

        public sealed override PartState State()
        {
            return null;
        }

        protected abstract Task<LlmAnswer> Request(string systemPrompt, IReadOnlyList<LlmMessage> messages);

        private string Prompted(string systemPrompt)
        {
            string known = string.IsNullOrEmpty(memory) ? "Пока пусто." : memory;
            return systemPrompt + "\n" +
                   "Твоя память:\n" +
                   known + "\n" +
                   "Правила памяти: в поле memory ответа можешь вернуть новую полную версию своей памяти, либо null, если менять нечего.\n" +
                   "В памяти держи только важное о себе и о мире вокруг.\n" +
                   "Не записывай в память факты о собеседнике: историю разговора с ним ты и так всегда видишь.\n" +
                   $"Держи память короче {MemoryLimit} символов, устаревшее выбрасывай.";
        }

        private void Remember(string update)
        {
            if (update == null) return;

            memory = update;
            Log.Info("Entity {} rewrote its memory ({} chars): {}", Self.Name, update.Length, update);
        }
    }
}
