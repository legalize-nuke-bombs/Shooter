using System;
using System.Linq;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Character))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Sleeper))]
    public class PlayerMouth : NetworkBehaviour, IMortal
    {
        public const float TalkReach = 8f;
        public const int MaxSpeechLength = 300;
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private readonly NetworkVariable<ulong> talkerNetworkObjectId = new();

        private Character character;
        private ConversationManager conversationManager;
        private Health health;
        private Sleeper sleeper;
        private Talker talker;

        public bool IsTalking => talker != null;

        public ulong TalkerNetworkObjectId => talkerNetworkObjectId.Value;

        public long CharacterId => character.Id;

        public event Action<ulong> TalkOpened;

        public event Action TalkClosed;

        private void Awake()
        {
            character = GetComponent<Character>();
            conversationManager = Registers.GetAll<ConversationManager>(RegisterInactivePolicy.Include).Single();
            health = GetComponent<Health>();
            sleeper = GetComponent<Sleeper>();
            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer) enabled = true;
        }

        public override void OnNetworkDespawn()
        {
            ForgetTalker();
            enabled = false;
        }

        private void OnDisable()
        {
            ForgetTalker();
            if (IsServer && IsSpawned) talkerNetworkObjectId.Value = 0;
        }

        public void HandleDeath()
        {
            CloseTalk();
        }

        public void OpenTalk(Talker talker)
        {
            if (!IsServer || this.talker == talker) return;

            if (!CanTalkTo(talker))
            {
                Log.Info($"Player {OwnerClientId} can not talk to {talker.name}: one of them is dead or asleep");
                return;
            }

            if (IsTalking) CloseTalk();

            this.talker = talker;
            talkerNetworkObjectId.Value = talker.NetworkObjectId;
            Log.Info($"Player {OwnerClientId} opened a talk with {talker.name}");

            RaiseTalkOpenedRpc(talker.NetworkObjectId);
        }

        public void CloseTalk()
        {
            if (!IsServer || !IsTalking) return;

            Log.Info($"Player {OwnerClientId} closed the talk with {talker.name}");
            ForgetTalker();
            talkerNetworkObjectId.Value = 0;
            RaiseTalkClosedRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void SayRpc(string speech)
        {
            if (!IsTalking)
            {
                Log.Info($"Player {OwnerClientId} said something with no talk open, ignored");
                return;
            }

            if (speech == null || speech.Length > MaxSpeechLength)
            {
                Log.Info($"Player {OwnerClientId} said over {MaxSpeechLength} characters, ignored");
                return;
            }

            conversationManager.Say(CharacterId, talker.CharacterId, speech, true, true);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void HangUpRpc()
        {
            CloseTalk();
        }

        private void Update()
        {
            if (!IsTalking) return;
            if (talker.isActiveAndEnabled && IsWithinTalkReach(talker) && CanTalkTo(talker)) return;

            Log.Info($"Player {OwnerClientId} ends the talk with {talker.name}: out of reach, dead or asleep");
            CloseTalk();
        }

        private void ForgetTalker()
        {
            if (!IsTalking) return;

            talker = null;
        }

        private bool CanTalkTo(Talker talker)
        {
            return TalkRule.CanTalk(health.IsAlive, !sleeper.IsSleeping, talker.IsAlive, !talker.IsSleeping);
        }

        private bool IsWithinTalkReach(Talker talker)
        {
            return Vector3.Distance(talker.transform.position, transform.position) <= TalkReach;
        }

        [Rpc(SendTo.Owner)]
        private void RaiseTalkOpenedRpc(ulong talkerId)
        {
            Log.Info($"Talk with network object {talkerId} opened");
            TalkOpened?.Invoke(talkerId);
        }

        [Rpc(SendTo.Owner)]
        private void RaiseTalkClosedRpc()
        {
            Log.Info("Talk closed");
            TalkClosed?.Invoke();
        }
    }
}
