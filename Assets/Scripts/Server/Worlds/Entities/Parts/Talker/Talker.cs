using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shooter.Logging;

namespace Shooter.Server.Worlds.Entities.Parts.Talker
{
    public abstract class Talker : Part
    {
        public const float TalkReach = 8f;
        public const int SpeechLimit = 300;

        private const float AnswerTimeout = 30f;

        private readonly Dictionary<long, Conversation> conversations = new Dictionary<long, Conversation>();
        private readonly Dictionary<long, float> awaited = new Dictionary<long, float>();
        private readonly ConcurrentQueue<Reply> replies = new ConcurrentQueue<Reply>();

        protected Talker(Entity self) : base(self, typeof(Talker))
        {
        }

        public bool TryToListen(Entity user, string content)
        {
            if (!Alive())
            {
                Log.Info("Entity {} can not talk while it is dead, ignored", Self.Name);
                return false;
            }

            Pilot.Pilot pilot = user.Get<Pilot.Pilot>();
            if (pilot == null)
            {
                Log.Info("Entity {} tried to talk to entity {} without a pilot, ignored", user.Name, Self.Name);
                return false;
            }

            if (content.Length > SpeechLimit)
            {
                Log.Info("Speech of entity {} is over {} characters, ignored", user.Name, SpeechLimit);
                return false;
            }

            long userId = pilot.UserId;
            if (!conversations.TryGetValue(userId, out Conversation conversation))
            {
                conversation = new Conversation(user);
                conversations.Add(userId, conversation);
                Log.Info("Entity {} started a conversation with user {}", Self.Name, userId);
            }

            conversation.Follow(user);

            Message last = conversation.Last();
            if (last != null && last.Author == MessageAuthor.Player)
            {
                Log.Info("User {} spoke to entity {} while the answer is pending, ignored", userId, Self.Name);
                return false;
            }

            conversation.Add(new Message { Author = MessageAuthor.Player, Content = content });
            Log.Info("Entity {} received a message from user {}", Self.Name, userId);
            return true;
        }

        public override void Tick(float dt)
        {
            Deliver();
            Expire(dt);

            if (!Alive()) return;

            foreach (KeyValuePair<long, Conversation> entry in conversations)
            {
                if (awaited.ContainsKey(entry.Key)) continue;

                Message last = entry.Value.Last();
                if (last == null || last.Author == MessageAuthor.Talker) continue;

                Ask(entry.Key, entry.Value);
            }
        }

        public override void Forget(long userId)
        {
            if (!conversations.Remove(userId)) return;

            awaited.Remove(userId);
            Log.Info("Entity {} forgot the conversation with user {}", Self.Name, userId);
        }

        public override PartState State()
        {
            return new TalkerState
            {
                Conversations = conversations.ToDictionary(entry => entry.Key, entry => entry.Value.State())
            };
        }

        protected abstract Task<string> Answer(Conversation conversation);

        private void Ask(long userId, Conversation conversation)
        {
            awaited.Add(userId, 0f);
            Log.Info("Entity {} is preparing an answer for user {}", Self.Name, userId);
            _ = Prepare(userId, conversation);
        }

        private async Task Prepare(long userId, Conversation conversation)
        {
            try
            {
                string content = await Answer(conversation);
                replies.Enqueue(new Reply { UserId = userId, Content = content });
            }
            catch (Exception e)
            {
                Log.Error("Entity {} failed to answer user {}: {}", Self.Name, userId, e.Message);
                replies.Enqueue(new Reply { UserId = userId, Content = null });
            }
        }

        private void Deliver()
        {
            while (replies.TryDequeue(out Reply reply))
            {
                awaited.Remove(reply.UserId);

                if (reply.Content == null) continue;

                if (!conversations.TryGetValue(reply.UserId, out Conversation conversation))
                {
                    Log.Info("Entity {} answered user {} whose conversation is gone, dropped", Self.Name, reply.UserId);
                    continue;
                }

                conversation.Add(new Message { Author = MessageAuthor.Talker, Content = reply.Content });
                Log.Info("Entity {} answered user {}", Self.Name, reply.UserId);
            }
        }

        private void Expire(float dt)
        {
            if (awaited.Count == 0) return;

            var expired = new List<long>();
            foreach (long userId in awaited.Keys.ToList())
            {
                float waited = awaited[userId] + dt;
                awaited[userId] = waited;
                if (waited >= AnswerTimeout) expired.Add(userId);
            }

            foreach (long userId in expired)
            {
                awaited.Remove(userId);
                Log.Warn("Entity {} gave up waiting for an answer to user {} after {}s", Self.Name, userId, AnswerTimeout);
            }
        }

        private bool Alive()
        {
            Health.Health health = Self.Get<Health.Health>();
            return health != null && health.Alive;
        }

        private struct Reply
        {
            public long UserId;
            public string Content;
        }
    }
}
