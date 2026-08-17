using System;
using System.Collections.Generic;
using System.Linq;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;
using Shooter.Game.World;

namespace Shooter.Game.Speech
{
    public abstract class Talker : NetworkBehaviour, IUsable, IRestraint
    {
        private static readonly Journal Log = Logs.Here();

        public const float TalkReach = 8f;
        public const int SpeechLimit = 300;

        private readonly HashSet<long> thinking = new HashSet<long>();
        private readonly Dictionary<long, Conversation> conversations = new Dictionary<long, Conversation>();

        public UsageType Usage => UsageType.Talk;

        public bool CanPerform(ActionType type, float dt)
        {
            return !conversations.Values.Any(c => c.Open);
        }
        public void RegisterAction(ActionType type, float dt) {}

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

            var mouth = user.GetComponentInChildren<Mouth>();
            if (mouth == null) return;

            if (mouth.Interlocutor == NetworkObjectId)
            {
                mouth.Close();
                return;
            }

            if (!TalkRule.CanTalk(Alive(user), Alive(NetworkObject), Awake(NetworkObject)))
            {
                Log.Info($"Entity {this.NameOf()} refused to talk to {user.name}: speaker alive {(Alive(user))}, own alive {(Alive(NetworkObject))}, own awake {(Awake(NetworkObject))}");
                return;
            }

            if (!user.TryGetComponent(out PersistentId speaker))
            {
                Log.Warn($"Entity {this.NameOf()} refused to talk to {user.name}: the speaker has no persistent id");
                return;
            }

            Conversation conversation = Remember(speaker.Value);
            conversation.Reopen();
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

            if (!user.TryGetComponent(out PersistentId speaker))
            {
                Log.Warn($"Entity {this.NameOf()} ignores speech of {user.name}: the speaker has no persistent id");
                return;
            }

            if (!conversations.TryGetValue(speaker.Value, out Conversation conversation) || !conversation.Open)
            {
                Log.Info($"Entity {user.name} spoke to {this.NameOf()} without an open talk, ignored");
                return;
            }

            if (thinking.Contains(speaker.Value))
            {
                Log.Info($"Entity {user.name} spoke to {this.NameOf()} while the answer is pending, ignored");
                return;
            }

            Say(conversation, MessageAuthor.Player, content);
        }

        public void Leave(NetworkObject user)
        {
            if (!IsServer) return;

            if (!user.TryGetComponent(out PersistentId speaker)) return;

            if (!conversations.TryGetValue(speaker.Value, out Conversation conversation)) return;

            conversation.Close();
            Forget(conversation.Wanderer);
        }

        protected abstract void RequestAnswer(long wandererId, string message, Action<string> onAnswer);

        protected virtual void Forget(long wandererId)
        {
        }

        private void DeliverAnswer(long wandererId, string content)
        {
            thinking.Remove(wandererId);

            if (!conversations.TryGetValue(wandererId, out Conversation conversation) || !conversation.Open)
            {
                Log.Info($"Entity {this.NameOf()} answered wanderer {wandererId} whose conversation is gone, dropped");
                return;
            }

            Say(conversation, MessageAuthor.Talker, content);
            Log.Info($"Entity {this.NameOf()} answered wanderer {wandererId}");
        }

        private void Step()
        {
            Watch();

            if (!Alive(NetworkObject)) return;

            foreach (KeyValuePair<long, Conversation> entry in conversations)
            {
                if (!entry.Value.Open || thinking.Contains(entry.Key)) continue;

                Message last = entry.Value.Last();
                if (last == null || last.Author == MessageAuthor.Talker) continue;

                thinking.Add(entry.Key);

                try
                {
                    RequestAnswer(entry.Key, last.Content, (answer) =>
                    {
                        DeliverAnswer(entry.Key, answer);
                    });
                }
                catch (Exception e)
                {
                    Log.Warn($"Entity {this.NameOf()} failed to request answer for wanderer {entry.Key}: {e.Message}");
                    DeliverAnswer(entry.Key, "Not now.");
                }
            }
        }

        private Conversation Remember(long wanderer)
        {
            if (conversations.TryGetValue(wanderer, out Conversation conversation)) return conversation;

            conversation = new Conversation(wanderer);
            conversations.Add(wanderer, conversation);
            Log.Info($"Entity {this.NameOf()} started a conversation with wanderer {wanderer}");
            return conversation;
        }

        private void Say(Conversation conversation, MessageAuthor author, string content)
        {
            var message = new Message
            {
                Author = author,
                Content = content,
                Time = Clock.Current == null ? string.Empty : Clock.Current.DateTime()
            };

            conversation.Add(message);
            UserOf(conversation.Wanderer)?.GetComponentInChildren<Mouth>()?.Hear(message);
        }

        private void Watch()
        {
            foreach (Conversation conversation in conversations.Values)
            {
                if (!conversation.Open) continue;

                NetworkObject user = UserOf(conversation.Wanderer);
                if (user != null && Reachable(user) && Alive(user) && Awake(user) && Alive(NetworkObject)) continue;

                Log.Info($"Entity {this.NameOf()} ends the talk with {(user == null ? "a gone wanderer" : user.name)}: out of reach, dead or asleep");

                thinking.Remove(conversation.Wanderer);

                conversation.Close();
                Forget(conversation.Wanderer);
                if (user != null) user.GetComponentInChildren<Mouth>()?.Close();
            }
        }

        private static NetworkObject UserOf(long wandererId)
        {
            if (Registers.Current == null) return null;

            PersistentId found = Registers.Current.Of<PersistentId>().Of(wandererId);
            return found == null ? null : found.GetComponentInParent<NetworkObject>();
        }

        private bool Reachable(NetworkObject user)
        {
            return Vector3.Distance(user.transform.position, transform.position) <= TalkReach;
        }

        private static bool Alive(NetworkObject body)
        {
            var health = body.GetComponent<Health>();
            return health != null && health.Alive;
        }

        private static bool Awake(NetworkObject body)
        {
            var sleeper = body.GetComponentInChildren<Sleeper>();
            return sleeper == null || !sleeper.Sleeping;
        }
    }
}
