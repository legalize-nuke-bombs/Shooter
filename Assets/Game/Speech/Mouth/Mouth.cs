using System;
using System.Collections.Generic;
using Unity.Netcode;
using Shooter.Game.Dying;
using Shooter.Logging;

namespace Shooter.Game.Talking
{
    public class Mouth : NetworkBehaviour, IMortal
    {
        private readonly NetworkVariable<ulong> interlocutor = new NetworkVariable<ulong>();

        private Talker heard;

        public event Action<ulong> Opened;

        public event Action<string, string, bool> Heard;

        public event Action Closed;

        public bool Talking => heard != null;

        public ulong Interlocutor => interlocutor.Value;

        public void Open(Talker talker, IReadOnlyList<Message> history)
        {
            if (!IsServer || heard == talker) return;

            if (heard != null) Close();

            heard = talker;
            interlocutor.Value = talker.NetworkObjectId;
            Log.Info("Player {} opened a talk with {}", OwnerClientId, talker.name);

            OpenedRpc(talker.NetworkObjectId);
            foreach (Message message in history)
                HeardRpc(message.Content, message.Time, message.Author == MessageAuthor.Player);
        }

        public void Close()
        {
            if (!IsServer || heard == null) return;

            Log.Info("Player {} closed the talk with {}", OwnerClientId, heard.name);
            heard.Leave(NetworkObject);
            heard = null;
            interlocutor.Value = 0;
            ClosedRpc();
        }

        public void Hear(Message message)
        {
            if (!IsServer) return;

            HeardRpc(message.Content, message.Time, message.Author == MessageAuthor.Player);
        }

        public void Died()
        {
            Close();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void SayRpc(string speech)
        {
            if (heard == null)
            {
                Log.Info("Player {} said something with no talk open, ignored", OwnerClientId);
                return;
            }

            heard.Listen(NetworkObject, speech);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void HangUpRpc()
        {
            Close();
        }

        [Rpc(SendTo.Owner)]
        private void OpenedRpc(ulong talkerId)
        {
            Log.Info("Talk with network object {} opened", talkerId);
            Opened?.Invoke(talkerId);
        }

        [Rpc(SendTo.Owner)]
        private void HeardRpc(string content, string time, bool mine)
        {
            Log.Info("Talk line at {} from {}: {}", time, mine ? "me" : "them", content);
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
