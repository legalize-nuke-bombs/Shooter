using System;
using System.Collections.Generic;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Logging;
using Unity.Netcode;

namespace Shooter.Game.Speech
{
    public class Mouth : NetworkBehaviour, IMortal
    {
        private static readonly Journal Log = Logs.Here();

        private readonly NetworkVariable<ulong> interlocutor = new();

        private long heardId = GameObjectRuntimeId.Default;

        public bool Talking => heardId != GameObjectRuntimeId.Default;

        public ulong Interlocutor => interlocutor.Value;

        public void Died()
        {
            Close();
        }

        public event Action<ulong> Opened;

        public event Action<string, string, bool> Heard;

        public event Action Closed;

        public void Open(Talker talker, IReadOnlyList<Message> history)
        {
            if (!IsServer) return;

            Character talkerCharacter = talker.GetComponent<Character>();
            if (talkerCharacter == null)
            {
                Log.Warn($"Player {OwnerClientId} can not talk to {talker.name}: the talker has no persistent id");
                return;
            }

            if (heardId == talkerCharacter.Id) return;

            if (Talking) Close();

            heardId = talkerCharacter.Id;
            interlocutor.Value = talker.NetworkObjectId;
            Log.Info($"Player {OwnerClientId} opened a talk with {talker.name}");

            OpenedRpc(talker.NetworkObjectId);
            foreach (Message message in history)
                HeardRpc(message.Content, message.Time, message.Author == MessageAuthor.Player);
        }

        public void Close()
        {
            if (!IsServer || !Talking) return;

            Log.Info($"Player {OwnerClientId} closed the talk with wanderer {heardId}");
            TalkerOf(heardId)?.Leave(NetworkObject);
            heardId = GameObjectRuntimeId.Default;
            interlocutor.Value = 0;
            ClosedRpc();
        }

        public void Hear(Message message)
        {
            if (!IsServer) return;

            HeardRpc(message.Content, message.Time, message.Author == MessageAuthor.Player);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void SayRpc(string speech)
        {
            Talker heard = TalkerOf(heardId);
            if (heard == null)
            {
                Log.Info($"Player {OwnerClientId} said something with no talk open, ignored");
                return;
            }

            heard.Listen(NetworkObject, speech);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void HangUpRpc()
        {
            Close();
        }

        private static Talker TalkerOf(long talkerId)
        {
            if (talkerId == GameObjectRuntimeId.Default || Registers.Current == null) return null;

            Character found = Character.Of(talkerId, Inactive.Exclude);
            return found == null ? null : found.GetComponentInChildren<Talker>();
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
