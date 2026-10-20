using System;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Character))]
    [RequireComponent(typeof(Health))]
    public abstract class Talker : ShooterNetworkBehaviour, IUsable
    {
        private readonly NetworkVariable<bool> thinking = new();

        private Character character;
        private Health health;
        private Sleeper sleeper;

        public bool IsThinking => thinking.Value;

        public long CharacterId => character.Id;

        public bool IsAlive => health.IsAlive;

        public bool IsSleeping => sleeper != null && sleeper.IsSleeping;

        public UsageType Usage => UsageType.Talk;

        public event Action<bool> ThinkingChanged;

        protected override void ShooterAwake()
        {
            character = GetComponent<Character>();
            health = GetComponent<Health>();
            sleeper = GetComponent<Sleeper>();
            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            thinking.OnValueChanged += RelayThinking;
            if (!IsServer) return;

            enabled = true;
        }

        public override void OnNetworkDespawn()
        {
            thinking.OnValueChanged -= RelayThinking;
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

        private void RelayThinking(bool previous, bool current)
        {
            ThinkingChanged?.Invoke(current);
        }
    }
}
