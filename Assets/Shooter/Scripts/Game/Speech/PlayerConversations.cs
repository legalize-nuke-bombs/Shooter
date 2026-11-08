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
    public class PlayerConversations : NetworkBehaviour
    {
        private const int MaxHistoryPageBytes = 60 * 1024;
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<long, Contact> contacts = new();

        private Character character;
        private Player player;
        private ConversationManager conversationManager;

        public long CharacterId => character.Id;

        public IReadOnlyDictionary<long, Contact> Contacts => contacts;

        public event Action<long> HistoryArrived;

        public event Action<long, ConversationLine> LiveLineArrived;

        public event Action ContactsCleared;

        private void Awake()
        {
            character = GetComponent<Character>();
            player = GetComponent<Player>();
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

            ReceiveLiveLineRpc(conversation.GetPartnerId(CharacterId), ConversationLine.FromMessage(message));
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void RequestHistoryRpc()
        {
            List<Conversation> conversations = conversationManager.GetConversationsOf(CharacterId);
            foreach (Conversation conversation in conversations)
            {
                SendHistory(conversation);
            }

            Log.Info($"Player {OwnerClientId} received the history of its {conversations.Count} conversations");
        }

        // Netcode refuses an RPC over 64 KB, so a long history goes in pages, all in this frame and in order
        private void SendHistory(Conversation conversation)
        {
            long partnerId = conversation.GetPartnerId(CharacterId);
            var page = new List<ConversationLine>();
            int pageBytes = 0;
            bool isFirstPage = true;
            foreach (ConversationLine line in conversation.Messages.Select(ConversationLine.FromMessage))
            {
                if (page.Count > 0 && pageBytes + line.SerializedSize > MaxHistoryPageBytes)
                {
                    ReceiveHistoryRpc(partnerId, isFirstPage, page.ToArray());
                    isFirstPage = false;
                    page.Clear();
                    pageBytes = 0;
                }

                page.Add(line);
                pageBytes += line.SerializedSize;
            }

            ReceiveHistoryRpc(partnerId, isFirstPage, page.ToArray());
        }

        [Rpc(SendTo.Owner)]
        private void ReceiveHistoryRpc(long partnerId, bool isFirstPage, ConversationLine[] lines)
        {
            Contact contact = GetOrCreateContact(partnerId);
            if (isFirstPage)
            {
                contact.ReplaceLines(lines);
            }
            else
            {
                contact.AddLines(lines);
            }

            HistoryArrived?.Invoke(partnerId);
        }

        [Rpc(SendTo.Owner)]
        private void ReceiveLiveLineRpc(long partnerId, ConversationLine line)
        {
            GetOrCreateContact(partnerId).AddLine(line);
            LiveLineArrived?.Invoke(partnerId, line);
        }
    }
}
