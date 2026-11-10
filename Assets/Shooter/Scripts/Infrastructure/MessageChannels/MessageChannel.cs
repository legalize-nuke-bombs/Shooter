using System;
using Unity.Netcode;

namespace Shooter.Infrastructure
{
    public sealed class MessageChannel<T> : IMessageChannel where T : INetworkSerializable, new()
    {
        private readonly NetworkBehaviour owner;
        private readonly MessageChannels channels;

        public MessageChannel(NetworkBehaviour owner)
        {
            this.owner = owner;
            channels = Registers.Instance.Get<MessageChannels>();
        }

        public event Action<T> Received;

        public void SendToServer(T message)
        {
            Send(NetworkManager.ServerClientId, message);
        }

        public void SendToOwner(T message)
        {
            Send(owner.OwnerClientId, message);
        }

        public void SendToClient(ulong clientId, T message)
        {
            Send(clientId, message);
        }

        public void SendToClients(T message)
        {
            foreach (ulong clientId in owner.NetworkManager.ConnectedClientsIds)
            {
                Send(clientId, message);
            }
        }

        public void Receive(FastBufferReader reader)
        {
            reader.ReadNetworkSerializable(out T message);
            Received?.Invoke(message);
        }

        private void Send(ulong clientId, T message)
        {
            if (clientId == owner.NetworkManager.LocalClientId)
            {
                Received?.Invoke(message);
                return;
            }

            channels.Send(owner, this, clientId, message);
        }
    }
}
