using System.Threading.Tasks;
using Shooter.Server.Worlds.Time;

namespace Shooter.Server.Worlds.Entities.Parts.Talker.AITalker
{
    public abstract class AITalker : Talker
    {
        private readonly Clock clock;
        private readonly string character;

        protected AITalker(Entity self, Clock clock, string character) : base(self)
        {
            this.clock = clock;
            this.character = character;
        }

        protected sealed override Task<string> Answer(Conversation conversation)
        {
            return RequestAnswer(TalkPrompt.System(Self, conversation, clock, character), TalkPrompt.Dialog(conversation));
        }

        protected abstract Task<string> RequestAnswer(string systemPrompt, string conversation);
    }
}
