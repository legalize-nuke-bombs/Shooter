using System;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Speech
{
    [RequireComponent(typeof(Character))]
    public class PlayerMouth : NetworkBehaviour, IMortal
    {
        public const float TalkReach = 8f;
        public const int SpeechLimit = 300;
        private static readonly Journal Log = Logs.Here();

        private readonly NetworkVariable<ulong> interlocutor = new();

        private Character character;
        private Talker talker;

        public bool Talking => talker != null;

        public ulong Interlocutor => interlocutor.Value;

        public long CharacterId => character.Id;

        public event Action<ulong> Opened;

        public event Action<string, string, bool> Heard;

        public event Action Closed;

        private void Awake()
        {
            character = GetComponent<Character>();
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
            talker.Engage(this);
            Log.Info($"Player {OwnerClientId} opened a talk with {talker.name}");

            OpenedRpc(talker.NetworkObjectId);

            ConversationManager conversations = ConversationManager.Current;
            if (conversations == null)
            {
                Log.Warn($"Player {OwnerClientId} starts blank: the world keeps no conversations");
                return;
            }

            foreach (Message message in conversations.Between(CharacterId, talker.CharacterId).Messages) Hear(message);
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

            ConversationManager conversations = ConversationManager.Current;
            if (conversations == null)
            {
                Log.Warn($"Player {OwnerClientId} speaks into the void: the world keeps no conversations");
                return;
            }

            Hear(conversations.Between(CharacterId, talker.CharacterId).Say(CharacterId, speech));
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
            if (talker == null) return;

            talker.Release(this);
            talker = null;
        }

        private bool Fit(Talker talker)
        {
            return TalkRule.CanTalk(Alive(NetworkObject), IsAwake(NetworkObject), Alive(talker.NetworkObject), IsAwake(talker.NetworkObject));
        }

        private bool Reachable(Talker talker)
        {
            return Vector3.Distance(talker.transform.position, transform.position) <= TalkReach;
        }

        private static bool Alive(NetworkObject body)
        {
            Health health = body.GetComponent<Health>();
            return health != null && health.Alive;
        }

        private static bool IsAwake(NetworkObject body)
        {
            Sleeper sleeper = body.GetComponent<Sleeper>();
            return sleeper == null || !sleeper.Sleeping;
        }

        [Rpc(SendTo.Owner)]
        private void OpenedRpc(ulong talkerId)
        {
            Log.Info($"Talk with network object {talkerId} opened");
            Opened?.Invoke(talkerId);
        }

        [Rpc(SendTo.Owner)]
        private void HeardRpc(string content, string time, bool mine)
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
