using System.Collections.Generic;
using System.Linq;
using Shooter.Logging;
using System;

namespace Shooter.Server.Worlds.Entities.Parts.Talker
{
    public abstract class Talker : Part
    {
        protected readonly Dictionary<long, Conversation> Conversations = new Dictionary<long, Conversation>();

        private readonly Health.Health health;

        public const float TalkReach = 8f;

        protected Talker(Health.Health health)
        {
            this.health = health;
        }

        public sealed override Type Slot => typeof(Talker);

        public bool CanTalkTo(long userId)
        {
            return health.Alive && true;
        }

        public bool TryToListen(long userId, string content)
        {
            if (!CanTalkTo(userId))
            {
                return false;
            }

            if (Conversations.TryAdd(userId, new Conversation()))
            {
                Log.Info("Conversation created with user {}", userId);
            }

            Conversations[userId].Add(new Message
            {
                Author = MessageAuthor.Player,
                Content = content
            });
            Log.Info("Received message from user {}: {}", userId, content); // TODO remove content from logs
            return true;
        }

        public override void Tick(Entity entity, float dt)
        {
            if (!health.Alive)
            {
                return;
            }

            foreach (long userId in Conversations.Keys)
            {
                Message last = Conversations[userId].Last();
                if (last == null || last.Author == MessageAuthor.Talker)
                {
                    return;
                }

                if (!CanTalkTo(userId))
                {
                    return;
                }

                StartTalking(userId);
            }
        }

        public abstract void StartTalking(long userId);

        public override PartState State()
        {
            return new TalkerState
            {
                Conversations =
                    Conversations.ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value.State())
            };
        }
    }
}
