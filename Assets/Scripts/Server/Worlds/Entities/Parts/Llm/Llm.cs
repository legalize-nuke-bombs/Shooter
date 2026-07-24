using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shooter.Logging;
using Shooter.Server.Protocol;

namespace Shooter.Server.Worlds.Entities.Parts.Llm
{
    public abstract class Llm : Part
    {
        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        private readonly string character;

        private string memory = "";

        protected Llm(Entity self, string character) : base(self, typeof(Llm))
        {
            this.character = character;
        }

        public async Task<string> Ask(string situation, IReadOnlyList<LlmMessage> messages)
        {
            if (gate.CurrentCount == 0)
            {
                Log.Info("Entity {} has an llm request in flight, waiting for a free slot", Self.Name);
            }

            await gate.WaitAsync();
            try
            {
                string systemPrompt = LlmPrompt.System(character, memory, Self.Digest(), situation);
                LlmAnswer answer = await Request(systemPrompt, messages);
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

        private void Remember(string update)
        {
            if (update == null) return;

            memory = update;
            Log.Info("Entity {} rewrote its memory ({} chars): {}", Self.Name, update.Length, update);
        }
    }
}
