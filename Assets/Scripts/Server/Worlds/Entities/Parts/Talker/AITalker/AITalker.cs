using System;
using System.Threading.Tasks;
using Shooter.Server.Worlds.Time;

namespace Shooter.Server.Worlds.Entities.Parts.Talker.AITalker
{
    public sealed class AITalker : Talker
    {
        private readonly Clock clock;
        private readonly string character;

        public AITalker(Entity self, Clock clock, string character) : base(self)
        {
            this.clock = clock;
            this.character = character;
        }

        protected override Task<string> Answer(Conversation conversation)
        {
            Llm.Llm llm = Self.Get<Llm.Llm>();
            if (llm == null)
            {
                throw new InvalidOperationException($"Entity {Self.Name} has no llm part to answer with");
            }

            return llm.Ask(TalkPrompt.System(Self, conversation, clock, character), TalkPrompt.Messages(conversation));
        }
    }
}
