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
            if (IsServer)
            {
                enabled = true;
            }
            else
            {
                thinking.OnValueChanged += AnnounceReplicatedThinking;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) thinking.OnValueChanged -= AnnounceReplicatedThinking;
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
            bool busy = IsBusy();
            if (busy == thinking.Value) return;

            thinking.Value = busy;
            ThinkingChanged?.Invoke(busy);
        }

        private void AnnounceReplicatedThinking(bool previous, bool current)
        {
            ThinkingChanged?.Invoke(current);
        }
    }
}
