using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Core.Saves;
using Shooter.Game.World;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Speech
{
    public abstract class Talker : NetworkBehaviour, IUsable, IRestraint, ISaveableComponent
    {
        public const float TalkReach = 8f;
        public const int SpeechLimit = 300;
        protected const string Fallback = "Not now.";
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<long, Conversation> conversations = new();
        private readonly NetworkVariable<bool> thinking = new();

        [SerializeField] private SoundSpec muttering;

        private Speaker speaker;

        public bool Thinking => thinking.Value;

        public event Action<bool> ThinkingChanged;

        public string ComponentKey => "Talker";
        private struct SaveData
        {
            public List<Conversation> Conversations { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData()
            {
                Conversations = conversations.Values.ToList()
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.To<SaveData>();
            conversations.Clear();
            foreach (Conversation conversation in sd.Conversations)
                conversations.Add(conversation.Wanderer, conversation);
        }

        private static bool Awake(NetworkObject body)
        {
            Sleeper sleeper = body.GetComponentInChildren<Sleeper>();
            return sleeper == null || !sleeper.Sleeping;
        }

        public bool CanPerform(ActionType type, float dt)
        {
            return !conversations.Values.Any(c => c.Open);
        }

        public void RegisterAction(ActionType type, float dt)
        {
        }

        public UsageType Usage => UsageType.Talk;

        public void Use(NetworkObject user)
        {
            if (!IsServer) return;

            Mouth mouth = user.GetComponentInChildren<Mouth>();
            if (mouth == null) return;

            if (mouth.Interlocutor == NetworkObjectId)
            {
                mouth.Close();
                return;
            }

            if (!TalkRule.CanTalk(Alive(user), Alive(NetworkObject), Awake(NetworkObject)))
            {
                Log.Info(
                    $"Entity {name} refused to talk to {user.name}: speaker alive {Alive(user)}, own alive {Alive(NetworkObject)}, own awake {Awake(NetworkObject)}");
                return;
            }

            if (!user.TryGetComponent(out Character speaker))
            {
                Log.Warn($"Entity {name} refused to talk to {user.name}: the speaker has no persistent id");
                return;
            }

            Conversation conversation = Remember(speaker.Id);
            conversation.Reopen();
            mouth.Open(this, conversation.Messages);
        }

        protected virtual void Awake()
        {
            speaker = GetComponent<Speaker>();
            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            thinking.OnValueChanged += RelayThinking;
            if (IsServer) enabled = true;
        }

        public override void OnNetworkDespawn()
        {
            thinking.OnValueChanged -= RelayThinking;
            enabled = false;
        }

        private void RelayThinking(bool previous, bool current)
        {
            ThinkingChanged?.Invoke(current);
        }

        public void Listen(NetworkObject user, string content)
        {
            if (!IsServer) return;

            if (content == null || content.Length > SpeechLimit)
            {
                Log.Info($"Speech of entity {user.name} is over {SpeechLimit} characters, ignored");
                return;
            }

            if (!user.TryGetComponent(out Character speaker))
            {
                Log.Warn($"Entity {name} ignores speech of {user.name}: the speaker has no persistent id");
                return;
            }

            Say(Remember(speaker.Id), MessageAuthor.Player, content);
            RequestAnswer(speaker.Id, content);
        }

        public void Leave(NetworkObject user)
        {
            if (!IsServer) return;

            if (!user.TryGetComponent(out Character speaker)) return;

            if (!conversations.TryGetValue(speaker.Id, out Conversation conversation)) return;

            conversation.Close();
        }

        protected abstract void RequestAnswer(long wandererId, string message);

        protected void DeliverAnswer(long wandererId, string content)
        {
            Say(Remember(wandererId), MessageAuthor.Talker, content ?? Fallback);
            if (muttering != null) speaker?.Play(muttering);
            Log.Info($"Entity {name} answered wanderer {wandererId}");
        }

        private void Update()
        {
            thinking.Value = Busy();
            Watch();
        }

        protected abstract bool Busy();

        private Conversation Remember(long wanderer)
        {
            if (conversations.TryGetValue(wanderer, out Conversation conversation)) return conversation;

            conversation = new Conversation(wanderer);
            conversations.Add(wanderer, conversation);
            Log.Info($"Entity {name} started a conversation with wanderer {wanderer}");
            return conversation;
        }

        private void Say(Conversation conversation, MessageAuthor author, string content)
        {
            var message = new Message
            {
                Author = author,
                Content = content,
                Time = Clock.Current == null
                    ? string.Empty
                    : Clock.Current.Now.ToString(Message.TimeFormat, CultureInfo.InvariantCulture)
            };

            conversation.Add(message);

            Mouth mouth = UserOf(conversation.Wanderer)?.GetComponentInChildren<Mouth>();
            if (mouth != null && mouth.Interlocutor == NetworkObjectId) mouth.Hear(message);
        }

        private void Watch()
        {
            foreach (Conversation conversation in conversations.Values)
            {
                if (!conversation.Open) continue;

                NetworkObject user = UserOf(conversation.Wanderer);
                if (user != null && Reachable(user) && Alive(user) && Awake(user) && Alive(NetworkObject)) continue;

                Log.Info(
                    $"Entity {name} ends the talk with {(user == null ? "a gone wanderer" : user.name)}: out of reach, dead or asleep");

                conversation.Close();
                if (user != null) user.GetComponentInChildren<Mouth>()?.Close();
            }
        }

        private static NetworkObject UserOf(long wandererId)
        {
            if (Registers.Current == null) return null;

            Character found = Character.Of(wandererId, Inactive.Exclude);
            return found == null ? null : found.GetComponentInParent<NetworkObject>();
        }

        private bool Reachable(NetworkObject user)
        {
            return Vector3.Distance(user.transform.position, transform.position) <= TalkReach;
        }

        private static bool Alive(NetworkObject body)
        {
            Health health = body.GetComponent<Health>();
            return health != null && health.Alive;
        }
    }
}
