using System.Collections.Generic;
using System.Linq;
using Shooter.Logging;
using System;

namespace Shooter.Server.Worlds.Entities.Parts.Talker
{
    public abstract class Talker : Part
    {
        public const float TalkReach = 8f;
        public const int SpeechLimit = 300;

        protected readonly Dictionary<long, Conversation> Conversations = new Dictionary<long, Conversation>();

        private readonly HashSet<long> answering = new HashSet<long>();
        protected readonly Health.Health Health;

        protected Talker(Health.Health health)
        {
            Health = health;
        }

        public sealed override Type Slot => typeof(Talker);

        public bool CanTalkTo(long userId)
        {
            return Health.Alive;
        }

        public bool TryToListen(long userId, string content)
        {
            if (!CanTalkTo(userId))
            {
                return false;
            }

            if (content.Length > SpeechLimit)
            {
                Log.Info("User {} speech is over {} characters, ignored", userId, SpeechLimit);
                return false;
            }

            if (Conversations.TryAdd(userId, new Conversation()))
            {
                Log.Info("Conversation created with user {}", userId);
            }

            Conversation conversation = Conversations[userId];
            Message last = conversation.Last();
            if (last != null && last.Author == MessageAuthor.Player)
            {
                Log.Info("User {} spoke while the answer is pending, ignored", userId);
                return false;
            }

            conversation.Add(new Message
            {
                Author = MessageAuthor.Player,
                Content = content
            });
            Log.Info("Received message from user {}: {}", userId, content); // TODO remove content from logs
            return true;
        }

        public override void Tick(Entity entity, float dt)
        {
            if (!Health.Alive)
            {
                return;
            }

            foreach (long userId in Conversations.Keys)
            {
                Message last = Conversations[userId].Last();
                if (last == null || last.Author == MessageAuthor.Talker)
                {
                    continue;
                }

                if (answering.Contains(userId) || !CanTalkTo(userId))
                {
                    continue;
                }

                answering.Add(userId);
                StartTalking(userId);
            }
        }

        protected abstract void StartTalking(long userId);

        protected void Say(long userId, string content)
        {
            answering.Remove(userId);
            Conversations[userId].Add(new Message
            {
                Author = MessageAuthor.Talker,
                Content = content
            });
            Log.Info("Talking to {}: {}", userId, content); // TODO remove content from logs
        }

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
