using System;
using System.Collections.Generic;
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
        private static readonly Journal Log = Logs.Here();

        private readonly List<PlayerMouth> engaged = new();
        private readonly NetworkVariable<bool> thinking = new();

        [SerializeField] private SoundSpec muttering;

        private Character character;
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
            if (IsServer) enabled = true;
        }

        public override void OnNetworkDespawn()
        {
            thinking.OnValueChanged -= RelayThinking;
            engaged.Clear();
            enabled = false;
        }

        public void Use(NetworkObject user)
        {
            if (!IsServer) return;
            if (!user.TryGetComponent(out PlayerMouth mouth)) return;

            if (mouth.Interlocutor == NetworkObjectId) mouth.Close();
            else mouth.Open(this);
        }

        public void Engage(PlayerMouth mouth)
        {
            if (!engaged.Contains(mouth)) engaged.Add(mouth);
        }

        public void Release(PlayerMouth mouth)
        {
            engaged.Remove(mouth);
        }

        public void Listen(PlayerMouth mouth, string content)
        {
            if (!IsServer) return;

            RequestAnswer(mouth.CharacterId, content);
        }

        public struct Answer
        {
            public string Content { get; set; }
            public bool Loud { get; set; }
        }

        protected void DeliverAnswer(long wandererId, Answer answer)
        {
            ConversationManager conversations = ConversationManager.Current;
            if (conversations == null)
            {
                Log.Warn($"Entity {name} answers wanderer {wandererId} into the void: the world keeps no conversations");
                return;
            }

            Message message = conversations.Between(CharacterId, wandererId).Say(CharacterId, answer.Content);
            foreach (PlayerMouth mouth in engaged)
                if (mouth.CharacterId == wandererId)
                    mouth.Hear(message);

            if (answer.Loud && muttering != null) speaker.Play(muttering);
            Log.Info($"Entity {name} answered wanderer {wandererId}");
        }

        protected abstract void RequestAnswer(long wandererId, string message);

        protected abstract bool Busy();

        private void Update()
        {
            thinking.Value = Busy();
        }

        private void RelayThinking(bool previous, bool current)
        {
            ThinkingChanged?.Invoke(current);
        }
    }
}
