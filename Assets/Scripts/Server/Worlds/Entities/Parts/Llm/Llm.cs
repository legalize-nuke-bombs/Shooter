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
                return await Request(systemPrompt, messages);
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

        protected abstract Task<string> Request(string systemPrompt, IReadOnlyList<LlmMessage> messages);
    }
}
