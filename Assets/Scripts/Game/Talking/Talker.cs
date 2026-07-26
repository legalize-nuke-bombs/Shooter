using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Shooter.Game.Interacting;
using Shooter.Game.Restraining;
using Shooter.Game.Sleeping;
using Shooter.Game.Vitals;
using Shooter.Logging;

namespace Shooter.Game.Talking
{
    public abstract class Talker : NetworkBehaviour, IUsable, IRestraint
    {
        public const float TalkReach = 8f;
        public const int SpeechLimit = 300;

        private const float AnswerTimeout = 30f;

        private readonly Dictionary<ulong, Conversation> conversations = new Dictionary<ulong, Conversation>();
        private readonly Dictionary<ulong, float> awaited = new Dictionary<ulong, float>();
        private readonly ConcurrentQueue<Reply> replies = new ConcurrentQueue<Reply>();

        public bool Restrains => conversations.Values.Any(conversation => conversation.Open);

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            NetworkManager.NetworkTickSystem.Tick += Step;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;

            NetworkManager.NetworkTickSystem.Tick -= Step;
        }

        public void Use(NetworkObject user)
        {
            if (!IsServer) return;

            var mouth = user.GetComponent<Mouth>();
            if (mouth == null) return;

            if (mouth.Interlocutor == NetworkObjectId)
            {
                mouth.Close();
                return;
            }

            if (!TalkRule.CanTalk(Alive(user), Alive(NetworkObject), Awake(NetworkObject)))
            {
                Log.Info("Entity {} refused to talk to {}: speaker alive {}, own alive {}, own awake {}",
                    name, user.name, Alive(user), Alive(NetworkObject), Awake(NetworkObject));
                return;
            }

            Conversation conversation = Remember(user);
            conversation.Reopen(user);
            mouth.Open(this, conversation.Messages);
        }

        public void Listen(NetworkObject user, string content)
        {
            if (!IsServer) return;

            if (content == null || content.Length > SpeechLimit)
            {
                Log.Info("Speech of entity {} is over {} characters, ignored", user.name, SpeechLimit);
                return;
            }

            if (!conversations.TryGetValue(user.OwnerClientId, out Conversation conversation) || !conversation.Open)
            {
                Log.Info("Entity {} spoke to {} without an open talk, ignored", user.name, name);
                return;
            }

            Message last = conversation.Last();
            if (last != null && last.Author == MessageAuthor.Player)
            {
                Log.Info("Entity {} spoke to {} while the answer is pending, ignored", user.name, name);
                return;
            }

            Say(conversation, user, MessageAuthor.Player, content);
        }

        public void Leave(NetworkObject user)
        {
            if (!IsServer) return;

            if (conversations.TryGetValue(user.OwnerClientId, out Conversation conversation))
                conversation.Close();
        }

        protected abstract Task<string> Answer(Conversation conversation);

        protected virtual string Fallback => "Not now.";

        private void Step()
        {
            Deliver();
            Expire(NetworkManager.LocalTime.FixedDeltaTime);
            Watch();

            if (!Alive(NetworkObject)) return;

            foreach (KeyValuePair<ulong, Conversation> entry in conversations)
            {
                if (!entry.Value.Open || awaited.ContainsKey(entry.Key)) continue;

                Message last = entry.Value.Last();
                if (last == null || last.Author == MessageAuthor.Talker) continue;

                Ask(entry.Key, entry.Value);
            }
        }

        private Conversation Remember(NetworkObject user)
        {
            if (conversations.TryGetValue(user.OwnerClientId, out Conversation conversation))
            {
                conversation.Follow(user);
                return conversation;
            }

            conversation = new Conversation(user);
            conversations.Add(user.OwnerClientId, conversation);
            Log.Info("Entity {} started a conversation with client {}", name, user.OwnerClientId);
            return conversation;
        }

        private void Say(Conversation conversation, NetworkObject user, MessageAuthor author, string content)
        {
            var message = new Message
            {
                Author = author,
                Content = content,
                Time = Environment.Current == null ? string.Empty : Environment.Current.Clock.DateTime()
            };

            conversation.Add(message);
            user.GetComponent<Mouth>()?.Hear(message);
        }

        private void Ask(ulong client, Conversation conversation)
        {
            awaited.Add(client, 0f);
            Log.Info("Entity {} is preparing an answer for client {}", name, client);
            _ = Prepare(client, conversation);
        }

        private async Task Prepare(ulong client, Conversation conversation)
        {
            try
            {
                string content = await Answer(conversation);
                replies.Enqueue(new Reply { Client = client, Content = content });
            }
            catch (Exception e)
            {
                Log.Error("Entity {} failed to answer client {}: {}", name, client, e.Message);
                replies.Enqueue(new Reply { Client = client, Content = Fallback });
            }
        }

        private void Deliver()
        {
            while (replies.TryDequeue(out Reply reply))
            {
                awaited.Remove(reply.Client);

                if (!conversations.TryGetValue(reply.Client, out Conversation conversation))
                {
                    Log.Info("Entity {} answered client {} whose conversation is gone, dropped", name, reply.Client);
                    continue;
                }

                Say(conversation, conversation.User, MessageAuthor.Talker, reply.Content);
                Log.Info("Entity {} answered client {}", name, reply.Client);
            }
        }

        private void Expire(float dt)
        {
            if (awaited.Count == 0) return;

            var expired = new List<ulong>();
            foreach (ulong client in awaited.Keys.ToList())
            {
                float waited = awaited[client] + dt;
                awaited[client] = waited;
                if (waited >= AnswerTimeout) expired.Add(client);
            }

            foreach (ulong client in expired)
            {
                awaited.Remove(client);
                Log.Warn("Entity {} gave up waiting for an answer to client {} after {}s", name, client, AnswerTimeout);
            }
        }

        private void Watch()
        {
            foreach (Conversation conversation in conversations.Values)
            {
                if (!conversation.Open) continue;

                NetworkObject user = conversation.User;
                if (user != null && Reachable(user) && Alive(user) && Awake(user) && Alive(NetworkObject)) continue;

                Log.Info("Entity {} ends the talk with {}: out of reach, dead or asleep", name, user == null ? "a gone player" : user.name);
                conversation.Close();
                user?.GetComponent<Mouth>()?.Close();
            }
        }

        private bool Reachable(NetworkObject user)
        {
            return Vector3.Distance(user.transform.position, transform.position) <= TalkReach;
        }

        private static bool Alive(NetworkObject entity)
        {
            var health = entity.GetComponent<Health>();
            return health != null && health.Alive;
        }

        private static bool Awake(NetworkObject entity)
        {
            var sleeper = entity.GetComponent<Sleeper>();
            return sleeper == null || !sleeper.Sleeping;
        }

        private struct Reply
        {
            public ulong Client;
            public string Content;
        }
    }
}
