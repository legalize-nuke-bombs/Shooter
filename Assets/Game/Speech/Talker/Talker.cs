using System;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Speech
{
    [RequireComponent(typeof(Character))]
    [RequireComponent(typeof(Speaker))]
    public abstract class Talker : NetworkBehaviour, IUsable
    {
        public const string Refusal = "Not now.";
        private static readonly Journal Log = Logs.Here();

        private readonly NetworkVariable<bool> thinking = new();

        [SerializeField] private SoundSpec muttering;

        private Character character;
        private ConversationManager conversations;
        private Speaker speaker;

        public bool Thinking => thinking.Value;

        public long CharacterId => character.Id;

        public UsageType Usage => UsageType.Talk;

        public event Action<bool> ThinkingChanged;

        protected virtual void Awake()
        {
            character = GetComponent<Character>();
            speaker = GetComponent<Speaker>();
            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            thinking.OnValueChanged += RelayThinking;
            if (!IsServer) return;

            enabled = true;
            conversations = ConversationManager.Current;
            if (conversations == null)
            {
                Log.Warn($"Entity {name} finds no conversations in the world and stays mute");
                return;
            }

            conversations.Said += Mutter;
        }

        public override void OnNetworkDespawn()
        {
            thinking.OnValueChanged -= RelayThinking;
            if (conversations != null) conversations.Said -= Mutter;
            conversations = null;
            enabled = false;
        }

        public void Use(NetworkObject user)
        {
            if (!IsServer) return;
            if (!user.TryGetComponent(out PlayerMouth mouth)) return;

            if (mouth.Interlocutor == NetworkObjectId) mouth.Close();
            else mouth.Open(this);
        }

        public void Listen(PlayerMouth mouth, string content)
        {
            if (!IsServer) return;

            RequestAnswer(mouth.CharacterId, content);
        }

        protected void Refuse(long wandererId)
        {
            if (conversations == null)
            {
                Log.Warn($"Entity {name} can not even refuse wanderer {wandererId}: the world keeps no conversations");
                return;
            }

            conversations.Say(CharacterId, wandererId, Refusal, false);
        }

        protected abstract void RequestAnswer(long wandererId, string message);

        protected abstract bool Busy();

        private void Update()
        {
            thinking.Value = Busy();
        }

        private void Mutter(Conversation conversation, Message message)
        {
            if (!message.Spoken || message.AuthorId != CharacterId || muttering == null) return;

            speaker.Play(muttering);
        }

        private void RelayThinking(bool previous, bool current)
        {
            ThinkingChanged?.Invoke(current);
        }
    }
}
