using System;
using System.Threading.Tasks;
using Shooter.Logging;

namespace Shooter.Server.Worlds.Entities.Parts.Talker.AITalker
{
    public abstract class AITalker : Talker
    {
        private const string FallbackAnswer = "Not now.";

        private readonly string characterSystemPrompt;
        private readonly AITalkerSettings settings = new AITalkerSettings();

        protected AITalker(string characterSystemPrompt, Health.Health health) : base(health)
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
                Log.Info("Requesting answer for user {}...", userId);
                answer = await RequestAnswer(settings.BaseSystemPrompt + "\n" + characterSystemPrompt, Conversations[userId].ToString());
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
