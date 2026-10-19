using System;
using System.Linq;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Character))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Speaker))]
    public abstract class Talker : NetworkBehaviour, IUsable
    {
        private readonly NetworkVariable<bool> thinking = new();

        [SerializeField] private SoundAsset muttering;

        private Character character;
        private ConversationManager conversationManager;
        private Health health;
        private Sleeper sleeper;
        private Speaker speaker;

        public bool IsThinking => thinking.Value;

        public long CharacterId => character.Id;

        public bool IsAlive => health.IsAlive;

        public bool IsSleeping => sleeper != null && sleeper.IsSleeping;

        public UsageType Usage => UsageType.Talk;

        public event Action<bool> ThinkingChanged;

        protected virtual void Awake()
        {
            character = GetComponent<Character>();
            health = GetComponent<Health>();
            sleeper = GetComponent<Sleeper>();
            speaker = GetComponent<Speaker>();
            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            thinking.OnValueChanged += RelayThinking;
            if (!IsServer) return;

            enabled = true;
            conversationManager = Registers.GetAll<ConversationManager>(RegisterInactivePolicy.Include).Single();
            conversationManager.Said += Mutter;
        }

        public override void OnNetworkDespawn()
        {
            thinking.OnValueChanged -= RelayThinking;
            if (IsServer) conversationManager.Said -= Mutter;
            conversationManager = null;
            enabled = false;
        }

        public void Use(NetworkObject user, out string promptResult)
        {
            if (!IsServer)
            {
                promptResult = "You are not a server";
                return;
            }

            if (!user.TryGetComponent(out PlayerMouth playerMouth))
            {
                promptResult = "Failed to talk: you are not a player";
                return;
            }

            if (playerMouth.TalkerNetworkObjectId == NetworkObjectId)
            {
                playerMouth.CloseTalk();
                promptResult = "Dialogue was closed";
            }
            else
            {
                playerMouth.OpenTalk(this);
                promptResult = "Dialogue was opened";
            }
        }

        protected abstract bool IsBusy();

        private void Update()
        {
            thinking.Value = IsBusy();
        }

        private void Mutter(Conversation conversation, Message message)
        {
            if (!message.Spoken || message.AuthorId != CharacterId || muttering == null) return;

            speaker.PlayForEveryone(muttering);
        }

        private void RelayThinking(bool previous, bool current)
        {
            ThinkingChanged?.Invoke(current);
        }
    }
}
