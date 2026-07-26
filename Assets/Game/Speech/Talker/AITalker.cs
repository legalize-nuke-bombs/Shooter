using System;
using System.Threading.Tasks;

namespace Shooter.Game.Talking
{
    public sealed class AITalker : Talker
    {
        private Llm.Llm llm;

        private void Awake()
        {
            llm = GetComponent<Llm.Llm>();
        }

        protected override Task<string> Answer(Conversation conversation)
        {
            if (llm == null)
            {
                throw new InvalidOperationException($"Entity {name} has no llm to answer with");
            }

            return llm.Ask(TalkPrompt.Situation(conversation.User), TalkPrompt.Messages(conversation));
        }
    }
}
