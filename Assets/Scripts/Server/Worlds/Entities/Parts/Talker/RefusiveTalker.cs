using Shooter.Logging;

namespace Shooter.Server.Worlds.Entities.Parts.Talker
{
    public class RefusiveTalker : Talker
    {

        public RefusiveTalker(Health.Health health) : base(health)
        {

        }

        public override void StartTalking(long userId)
        {
            var message = new Message
            {
                Author = MessageAuthor.Talker,
                Content = "I can't talk right now."
            };
            Conversations[userId].Add(message);
            Log.Info("Talking to {}: {}", userId, message.Content); // TODO remove Content from logs
        }
    }
}
