using System;
using System.Threading.Tasks;
using Shooter.Server.Worlds.Time;

namespace Shooter.Server.Worlds.Entities.Parts.Talker.AITalker
{
    public sealed class AITalker : Talker
    {
        public AITalker(Entity self, Clock clock) : base(self, clock)
        {
        }

        protected override Task<string> Answer(Conversation conversation)
        {
            Llm.Llm llm = Self.Get<Llm.Llm>();
            if (llm == null)
            {
                throw new InvalidOperationException($"Entity {Self.Name} has no llm part to answer with");
            }

            return llm.Ask(TalkPrompt.Situation(conversation.User), TalkPrompt.Messages(conversation));
        }
    }
}
