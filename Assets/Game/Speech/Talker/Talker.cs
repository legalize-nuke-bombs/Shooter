using System;
using System.Collections.Generic;
using System.Linq;
using Shooter.Game.Body;
using Shooter.Game.Body.Sleeping;
using Shooter.Game.Identity;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Speech
{
    public abstract class Talker : NetworkBehaviour, IUsable, IRestraint
    {
        private static readonly Journal Log = Logs.Here();

        public const float TalkReach = 8f;
        public const int SpeechLimit = 300;

        private readonly HashSet<ulong> thinking = new HashSet<ulong>();
        private readonly Dictionary<ulong, Conversation> conversations = new Dictionary<ulong, Conversation>();

        public UsageType Usage => UsageType.Talk;
        public bool Restrains => conversations.Values.Any(c => c.Open);

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
                Log.Info($"Entity {name} refused to talk to {user.name}: speaker alive {(Alive(user))}, own alive {(Alive(NetworkObject))}, own awake {(Awake(NetworkObject))}");
                return;
            }

            if (!user.TryGetComponent(out PersistentId _))
            {
                Log.Warn($"Entity {name} refused to talk to {user.name}: the speaker has no persistent id");
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
                Log.Info($"Speech of entity {user.name} is over {SpeechLimit} characters, ignored");
                return;
            }

            if (!conversations.TryGetValue(user.OwnerClientId, out Conversation conversation) || !conversation.Open)
            {
                Log.Info($"Entity {user.name} spoke to {name} without an open talk, ignored");
                return;
            }

            if (thinking.Contains(user.OwnerClientId))
            {
                Log.Info($"Entity {user.name} spoke to {name} while the answer is pending, ignored");
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

        protected abstract void RequestAnswer(long wandererId, string message, Action<string> onAnswer);

        private void DeliverAnswer(ulong clientId, string content)
        {
            thinking.Remove(clientId);

            if (content == null)
            {
                Log.Info($"Entity {name} kept silent towards client {clientId}");
                return;
            }

            if (!conversations.TryGetValue(clientId, out Conversation conversation) || !conversation.Open)
            {
                Log.Info($"Entity {name} answered client {clientId} whose conversation is gone, dropped");
                return;
            }

            Say(conversation, conversation.User, MessageAuthor.Talker, content);
            Log.Info($"Entity {name} answered client {clientId}");
        }

        private void Step()
        {
            Watch();

            if (!Alive(NetworkObject)) return;

            foreach (KeyValuePair<ulong, Conversation> entry in conversations)
            {
                if (!entry.Value.Open || thinking.Contains(entry.Key)) continue;

                Message last = entry.Value.Last();
                if (last == null || last.Author == MessageAuthor.Talker) continue;

                if (!entry.Value.User.TryGetComponent(out PersistentId speaker))
                {
                    Log.Warn($"Entity {name} can not answer {entry.Value.User.name}: the speaker has no persistent id");
                    continue;
                }

                thinking.Add(entry.Key);

                try
                {
                    RequestAnswer(speaker.Value, last.Content, (answer) =>
                    {
                        DeliverAnswer(entry.Key, answer);
                    });
                }
                catch (Exception e)
                {
                    Log.Warn($"Entity {name} failed to request answer for client {entry.Key}: {e.Message}");
                    DeliverAnswer(entry.Key, "Not now.");
                }
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
            Log.Info($"Entity {name} started a conversation with client {user.OwnerClientId}");
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

        private void Watch()
        {
            foreach (Conversation conversation in conversations.Values)
            {
                if (!conversation.Open) continue;

                NetworkObject user = conversation.User;
                if (user != null && Reachable(user) && Alive(user) && Awake(user) && Alive(NetworkObject)) continue;

                Log.Info($"Entity {name} ends the talk with {(user == null ? "a gone player" : user.name)}: out of reach, dead or asleep");

                if (user != null) thinking.Remove(user.OwnerClientId);

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
    }
}
