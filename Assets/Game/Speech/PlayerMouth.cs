using System;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Speech
{
    [RequireComponent(typeof(Character))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Sleeper))]
    public class PlayerMouth : NetworkBehaviour, IMortal
    {
        public const float TalkReach = 8f;
        public const int SpeechLimit = 300;
        private static readonly Journal Log = Logs.Here();

        private readonly NetworkVariable<ulong> interlocutor = new();

        private Character character;
        private ConversationManager conversations;
        private Health health;
        private Sleeper sleeper;
        private Talker talker;

        public bool Talking => talker != null;

        public ulong Interlocutor => interlocutor.Value;

        public long CharacterId => character.Id;

        public event Action<ulong> Opened;

        public event Action<string, DateTime, bool> Heard;

        public event Action Closed;

        private void Awake()
        {
            character = GetComponent<Character>();
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
            Forget();
            enabled = false;
        }

        private void OnDisable()
        {
            Forget();
            if (IsServer && IsSpawned) interlocutor.Value = 0;
        }

        public void Died()
        {
            Close();
        }

        public void Open(Talker talker)
        {
            if (!IsServer || this.talker == talker) return;

            if (!Fit(talker))
            {
                Log.Info($"Player {OwnerClientId} can not talk to {talker.name}: one of them is dead or asleep");
                return;
            }

            if (Talking) Close();

            this.talker = talker;
            interlocutor.Value = talker.NetworkObjectId;
            Log.Info($"Player {OwnerClientId} opened a talk with {talker.name}");

            OpenedRpc(talker.NetworkObjectId);

            conversations = ConversationManager.Current;
            foreach (Message message in conversations.Between(CharacterId, talker.CharacterId).Messages) Hear(message);
            conversations.Said += Relay;
        }

        public void Close()
        {
            if (!IsServer || !Talking) return;

            Log.Info($"Player {OwnerClientId} closed the talk with {talker.name}");
            Forget();
            interlocutor.Value = 0;
            ClosedRpc();
        }

        public void Hear(Message message)
        {
            if (!IsServer) return;

            HeardRpc(message.Content, message.Time, message.AuthorId == CharacterId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void SayRpc(string speech)
        {
            if (!Talking)
            {
                Log.Info($"Player {OwnerClientId} said something with no talk open, ignored");
                return;
            }

            if (speech == null || speech.Length > SpeechLimit)
            {
                Log.Info($"Player {OwnerClientId} said over {SpeechLimit} characters, ignored");
                return;
            }

            conversations.Say(CharacterId, talker.CharacterId, speech, true);
            talker.Listen(this, speech);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void HangUpRpc()
        {
            Close();
        }

        private void Update()
        {
            if (!Talking) return;
            if (talker.isActiveAndEnabled && Reachable(talker) && Fit(talker)) return;

            Log.Info($"Player {OwnerClientId} ends the talk with {talker.name}: out of reach, dead or asleep");
            Close();
        }

        private void Forget()
        {
            if (!Talking) return;

            conversations.Said -= Relay;
            conversations = null;
            talker = null;
        }

        private void Relay(Conversation conversation, Message message)
        {
            if (conversation.Key != Conversation.Pair(CharacterId, talker.CharacterId)) return;

            Hear(message);
        }

        private bool Fit(Talker talker)
        {
            return TalkRule.CanTalk(health.Alive, !sleeper.Sleeping, talker.Alive, !talker.Sleeping);
        }

        private bool Reachable(Talker talker)
        {
            return Vector3.Distance(talker.transform.position, transform.position) <= TalkReach;
        }

        [Rpc(SendTo.Owner)]
        private void OpenedRpc(ulong talkerId)
        {
            Log.Info($"Talk with network object {talkerId} opened");
            Opened?.Invoke(talkerId);
        }

        [Rpc(SendTo.Owner)]
        private void HeardRpc(string content, DateTime time, bool mine)
        {
            Log.Info($"Talk line at {time} from {(mine ? "me" : "them")}: {content}");
            Heard?.Invoke(content, time, mine);
        }

        [Rpc(SendTo.Owner)]
        private void ClosedRpc()
        {
            Log.Info("Talk closed");
            Closed?.Invoke();
        }
    }
}
