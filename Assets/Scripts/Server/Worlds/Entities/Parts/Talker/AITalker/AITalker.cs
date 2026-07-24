using System;
using System.Threading.Tasks;
using Shooter.Logging;
using Shooter.Server.Worlds.Items;

namespace Shooter.Server.Worlds.Entities.Parts.Talker.AITalker
{
    public abstract class AITalker : Talker
    {
        private const string FallbackAnswer = "Not now.";

        private readonly string characterSystemPrompt;
        private readonly AITalkerSettings settings = new AITalkerSettings();

        protected AITalker(Guid selfId, ServerWorld world, string characterSystemPrompt) : base(selfId, world)
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
                answer = await RequestAnswer(
                    BuildSystemPrompt(userId),
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

        private string BuildSystemPrompt(long userId)
        {
            string datetime = World.Clock().DateTime();

            Entity self = World.EntityById(SelfId);
            Health.Health health = (self == null ? null : self.Get<Health.Health>());
            string hp = (health == null ? "" : health.Hp.ToString());
            string maxHp = (health == null ? "" : health.MaxHp.ToString());

            Entity user = World.EntityByUserId(userId);
            Nameable.Nameable userNameable = (user == null ? null : user.Get<Nameable.Nameable>());
            Health.Health userHealth = (user == null ? null : user.Get<Health.Health>());
            Inventory.Inventory userInventory = (user == null ? null : user.Get<Inventory.Inventory>());
            string userName = (userNameable == null ? "" : userNameable.Payload);
            string userHp = (userHealth == null ? "" : userHealth.Hp.ToString());
            string userMaxHp = (userHealth == null ? "" : userHealth.MaxHp.ToString());
            UniqueItem userEquippedItemRaw = (userInventory == null ? null : userInventory.Equipped());
            string userEquippedItem = (userEquippedItemRaw == null ? "-" : userEquippedItemRaw.GetType().Name);

            string baseSystemPrompt = settings.BaseSystemPrompt(
                datetime,
                hp,
                maxHp,
                userName,
                userHp,
                userMaxHp,
                userEquippedItem
            );

            if (health == null || userNameable == null || userHealth == null || userInventory == null)
            {
                Log.Warn("Failed to build base system prompt, using fallback: {}", baseSystemPrompt);
            }

            return baseSystemPrompt + "\n" + characterSystemPrompt;
        }

        private string BuildConversationPrompt(long userId)
        {
            return Conversations[userId].ToString();
        }

        protected abstract Task<string> RequestAnswer(string systemPrompt, string conversation);
    }
}
