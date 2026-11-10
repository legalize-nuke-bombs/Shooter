using System;
using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Character))]
    [RequireComponent(typeof(Player))]
    public class PlayerConversations : NetworkBehaviour, IConversationListener
    {
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<long, Contact> contacts = new();

        private Character character;
        private Player player;
        private ConversationManager conversationManager;
        private MessageChannel<History> histories;
        private MessageChannel<LiveLine> liveLines;

        private struct History : INetworkSerializable
        {
            public long PartnerId;
            public ConversationLine[] Lines;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref PartnerId);
                serializer.SerializeValue(ref Lines);
            }
        }

        private struct LiveLine : INetworkSerializable
        {
            public long PartnerId;
            public ConversationLine Line;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref PartnerId);
                serializer.SerializeValue(ref Line);
            }
        }

        public long CharacterId => character.Id;

        public IReadOnlyDictionary<long, Contact> Contacts => contacts;

        public event Action<long> HistoryArrived;

        public event Action<long, ConversationLine> LiveLineArrived;

        public event Action ContactsCleared;

        private void Awake()
        {
            character = GetComponent<Character>();
            player = GetComponent<Player>();
            histories = new MessageChannel<History>(this);
            liveLines = new MessageChannel<LiveLine>(this);
            histories.Received += HandleHistoryReceived;
            liveLines.Received += HandleLiveLineReceived;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                conversationManager = Registers.Instance.Get<ConversationManager>();
                conversationManager.Said += HandleSaid;
            }

            player.ControlChanged += HandleControlChanged;
            HandleControlChanged();
        }

        public override void OnNetworkDespawn()
        {
            player.ControlChanged -= HandleControlChanged;

            if (conversationManager != null)
            {
                conversationManager.Said -= HandleSaid;
                conversationManager = null;
            }

            ClearContacts();
        }

        public Contact GetOrCreateContact(long partnerId)
        {
            if (!contacts.TryGetValue(partnerId, out Contact contact))
            {
                contact = new Contact(partnerId);
                contacts.Add(partnerId, contact);
            }

            return contact;
        }

        private void HandleControlChanged()
        {
            if (player.IsControlledLocally)
            {
                RequestHistoryRpc();
            }
            else
            {
                ClearContacts();
            }
        }

        private void ClearContacts()
        {
            contacts.Clear();
            ContactsCleared?.Invoke();
        }

        private void HandleSaid(Conversation conversation, Message message)
        {
            if (conversation.First != CharacterId && conversation.Second != CharacterId) return;
            if (!player.IsControlled) return;

            liveLines.SendToOwner(new LiveLine
            {
                PartnerId = conversation.GetPartnerId(CharacterId),
                Line = ConversationLine.FromMessage(message)
            });
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void RequestHistoryRpc()
        {
            List<Conversation> conversations = conversationManager.GetConversationsOf(CharacterId);
            foreach (Conversation conversation in conversations)
            {
                histories.SendToOwner(new History
                {
                    PartnerId = conversation.GetPartnerId(CharacterId),
                    Lines = conversation.Messages.Select(ConversationLine.FromMessage).ToArray()
                });
            }

            Log.Info($"Player {OwnerClientId} is sent the history of its {conversations.Count} conversations");
        }

        private void HandleHistoryReceived(History history)
        {
            GetOrCreateContact(history.PartnerId).ReplaceLines(history.Lines);
            HistoryArrived?.Invoke(history.PartnerId);
            Log.Info($"History with {history.PartnerId} arrived, {history.Lines.Length} lines");
        }

        private void HandleLiveLineReceived(LiveLine liveLine)
        {
            GetOrCreateContact(liveLine.PartnerId).AddLine(liveLine.Line);
            LiveLineArrived?.Invoke(liveLine.PartnerId, liveLine.Line);
            Log.Info($"A line with {liveLine.PartnerId} arrived");
        }
    }
}
