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

        private readonly Clock clock;

        protected AITalker(string characterSystemPrompt, Health.Health health, Clock clock) : base(health)
        {
            this.characterSystemPrompt = characterSystemPrompt;
            this.clock = clock;
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
                answer = await RequestAnswer(
                    BuildSystemPrompt(),
                    BuildConversationPrompt(userId)
                );
            }
            catch (Exception e)
            {
                Log.Error("Request for user {} failed: {}", userId, e.Message);
                answer = FallbackAnswer;
            }

            Say(userId, answer);
        }

        private string BuildSystemPrompt()
        {
            return
                settings.BaseSystemPrompt(clock.DateTime(), Health.Hp, Health.MaxHp) + "\n" +
                characterSystemPrompt;
        }

        private string BuildConversationPrompt(long userId)
        {
            return Conversations[userId].ToString();
        }

        protected abstract Task<string> RequestAnswer(string systemPrompt, string conversation);
    }
}
