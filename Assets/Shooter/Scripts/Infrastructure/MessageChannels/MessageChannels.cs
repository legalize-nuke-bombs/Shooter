using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkManager))]
    public class MessageChannels : MonoBehaviour
    {
        private const string MessageName = "Shooter.MessageChannels";
        private const int InitialMessageBytes = 1024;
        private static readonly Journal Log = Logs.Here();
        private static readonly Dictionary<Type, Dictionary<uint, FieldInfo>> ChannelFieldsByType = new();

        private NetworkManager network;

        private void Awake()
        {
            network = GetComponent<NetworkManager>();
            network.OnServerStarted += ListenForMessages;
            network.OnClientStarted += ListenForMessages;
        }

        private void OnDestroy()
        {
            network.OnServerStarted -= ListenForMessages;
            network.OnClientStarted -= ListenForMessages;
        }

        public void Send<T>(NetworkBehaviour owner, IMessageChannel channel, ulong clientId, in T message) where T : INetworkSerializable
        {
            if (!TryFindChannelId(owner, channel, out uint channelId))
            {
                Log.Error($"A channel of {owner.GetType().Name} {owner.name} is not a field of it, the message is not sent");
                return;
            }

            if (network.IsServer && !network.ConnectedClients.ContainsKey(clientId))
            {
                Log.Info($"Client {clientId} is not connected, a message of {owner.GetType().Name} {owner.name} to it is not sent");
                return;
            }

            using FastBufferWriter writer = new(InitialMessageBytes, Allocator.Persistent, int.MaxValue);
            writer.WriteValueSafe(owner.NetworkObjectId);
            writer.WriteValueSafe(owner.NetworkBehaviourId);
            writer.WriteValueSafe(channelId);
            writer.WriteNetworkSerializable(message);
            network.CustomMessagingManager.SendNamedMessage(MessageName, clientId, writer, NetworkDelivery.ReliableFragmentedSequenced);
        }

        private void ListenForMessages()
        {
            network.CustomMessagingManager.RegisterNamedMessageHandler(MessageName, HandleMessageReceived);
        }

        private void HandleMessageReceived(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out ulong networkObjectId);
            reader.ReadValueSafe(out ushort networkBehaviourId);
            reader.ReadValueSafe(out uint channelId);

            if (!network.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
            {
                Log.Info($"Object {networkObjectId} is not spawned here, a message to it from client {senderClientId} is dropped");
                return;
            }

            NetworkBehaviour owner = networkObject.GetNetworkBehaviourAtOrderIndex(networkBehaviourId);
            if (owner == null) return;

            if (network.IsServer && senderClientId != owner.OwnerClientId)
            {
                Log.Warn($"Client {senderClientId} sent a message to {owner.GetType().Name} {owner.name} it does not own, the message is dropped");
                return;
            }

            if (!GetChannelFields(owner.GetType()).TryGetValue(channelId, out FieldInfo field))
            {
                Log.Error($"{owner.GetType().Name} {owner.name} has no channel {channelId}, a message to it from client {senderClientId} is dropped");
                return;
            }

            ((IMessageChannel)field.GetValue(owner)).Receive(reader);
        }

        private static bool TryFindChannelId(NetworkBehaviour owner, IMessageChannel channel, out uint channelId)
        {
            foreach (KeyValuePair<uint, FieldInfo> entry in GetChannelFields(owner.GetType()))
            {
                if (!ReferenceEquals(entry.Value.GetValue(owner), channel)) continue;

                channelId = entry.Key;
                return true;
            }

            channelId = default;
            return false;
        }

        private static Dictionary<uint, FieldInfo> GetChannelFields(Type ownerType)
        {
            if (ChannelFieldsByType.TryGetValue(ownerType, out Dictionary<uint, FieldInfo> fields)) return fields;

            fields = new Dictionary<uint, FieldInfo>();
            for (Type type = ownerType; type != typeof(NetworkBehaviour); type = type.BaseType)
            {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (typeof(IMessageChannel).IsAssignableFrom(field.FieldType)) fields.Add(ChannelIdOf(field), field);
                }
            }

            ChannelFieldsByType.Add(ownerType, fields);
            return fields;
        }

        // string.GetHashCode may differ between Mono on the Mac and IL2CPP on Windows, and a channel needs one id on both
        private static uint ChannelIdOf(FieldInfo field)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(field.DeclaringType.FullName + "\0" + field.Name));
            return BitConverter.ToUInt32(hash, 0);
        }
    }
}
