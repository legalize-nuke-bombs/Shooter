using System;
using System.Threading.Tasks;
using Shooter.Logging;
using Shooter.Server.Worlds.Time;

namespace Shooter.Server.Worlds.Entities.Parts.Talker.AITalker
{
    public abstract class AITalker : Talker
    {
        private const string FallbackAnswer = "Not now.";

        private readonly string characterSystemPrompt;
        private readonly AITalkerSettings settings = new AITalkerSettings();

        protected AITalker(string characterSystemPrompt, Health.Health health, Clock clock) : base(health, clock)
        {
            this.characterSystemPrompt = characterSystemPrompt;
        }

        protected override void StartTalking(long userId)
        {
            _ = AnswerAsync(userId);
        }

        private async Task AnswerAsync(long userId)
        {
            string answer;
            try
            {
                string systemPrompt = settings.BaseSystemPrompt + "\n" + characterSystemPrompt;
                string conversation = Conversations[userId].Prompt();
                Log.Info("Requesting answer for user {} systemPrompt {} conversation {}...", userId, systemPrompt, conversation); // TODO remove message content
                answer = await RequestAnswer(systemPrompt, conversation);
            }
            catch (Exception e)
            {
                Log.Error("Request for user {} failed: {}", userId, e.Message);
                answer = FallbackAnswer;
            }

            Say(userId, answer);
        }

        protected abstract Task<string> RequestAnswer(string systemPrompt, string conversation);
    }
}
