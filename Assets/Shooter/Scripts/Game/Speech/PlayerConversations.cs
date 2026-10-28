using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Character))]
    public class PlayerConversations : NetworkBehaviour, IShooterUpdateable
    {
        private const int MaxBatchBytes = 16 * 1024;
        private const int LineOverheadBytes = 32;
        private const int BytesPerCharacter = 2;
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<long, Contact> contacts = new();

        private Character character;
        private ConversationManager conversationManager;
        private Coroutine sendConversationsCoroutine;
        private bool conversationsRequested;

        public long CharacterId => character.Id;

        public IReadOnlyDictionary<long, Contact> Contacts => contacts;

        public event Action<long> LinesArrived;

        public event Action<long, ConversationLine> LiveLineArrived;

        public event Action ContactsCleared;

        private void Awake()
        {
            character = GetComponent<Character>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            conversationManager = Registers.Instance.Get<ConversationManager>();
            conversationManager.Said += HandleSaid;
        }

        public override void OnNetworkDespawn()
        {
            if (conversationManager != null)
            {
                conversationManager.Said -= HandleSaid;
                conversationManager = null;
            }

            if (sendConversationsCoroutine != null)
            {
                StopCoroutine(sendConversationsCoroutine);
                sendConversationsCoroutine = null;
            }

            ClearContacts();
        }

        public override void OnGainedOwnership()
        {
            ClearContacts();
        }

        public override void OnLostOwnership()
        {
            ClearContacts();
        }

        public void ShooterUpdate()
        {
            // The owner pulls its history itself: one path for a fresh body, a reclaimed one and the host,
            // whose own body never changes owner and so never raises an ownership event
            if (!IsOwner || conversationsRequested) return;

            conversationsRequested = true;
            RequestConversationsRpc();
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

        private void ClearContacts()
        {
            contacts.Clear();
            conversationsRequested = false;
            ContactsCleared?.Invoke();
        }

        private void HandleSaid(Conversation conversation, Message message)
        {
            if (conversation.First != CharacterId && conversation.Second != CharacterId) return;

            // An offline body is owned by the host; the line comes with the rest of its conversations when its owner returns
            if (!gameObject.activeInHierarchy) return;

            ReceiveLiveLineRpc(conversation.GetPartnerId(CharacterId), conversation.IndexOf(message), ConversationLine.FromMessage(message));
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void RequestConversationsRpc()
        {
            Log.Info($"Player {OwnerClientId} asks for its conversations");

            if (sendConversationsCoroutine != null) StopCoroutine(sendConversationsCoroutine);
            sendConversationsCoroutine = StartCoroutine(SendConversationsInBatches());
        }

        // One batch per frame: an overflowing reliable queue drops the connection
        private IEnumerator SendConversationsInBatches()
        {
            int sentLineCount = 0;
            foreach (Conversation conversation in conversationManager.GetConversationsOf(CharacterId))
            {
                long partnerId = conversation.GetPartnerId(CharacterId);
                IReadOnlyList<Message> messages = conversation.Messages;
                int messageCount = messages.Count;

                var batch = new List<ConversationLine>();
                int firstIndex = 0;
                int batchBytes = 0;
                for (int i = 0; i < messageCount; i++)
                {
                    ConversationLine line = ConversationLine.FromMessage(messages[i]);
                    batch.Add(line);
                    batchBytes += LineOverheadBytes + BytesPerCharacter * (line.Content?.Length ?? 0);

                    if (batchBytes < MaxBatchBytes && i < messageCount - 1) continue;

                    ReceiveLinesRpc(partnerId, firstIndex, batch.ToArray());
                    sentLineCount += batch.Count;
                    firstIndex = i + 1;
                    batch.Clear();
                    batchBytes = 0;
                    yield return null;
                }
            }

            Log.Info($"Player {OwnerClientId} received its conversations: {sentLineCount} lines");
            sendConversationsCoroutine = null;
        }

        [Rpc(SendTo.Owner)]
        private void ReceiveLinesRpc(long partnerId, int firstIndex, ConversationLine[] lines)
        {
            Contact contact = GetOrCreateContact(partnerId);
            for (int i = 0; i < lines.Length; i++)
            {
                contact.PutLine(firstIndex + i, lines[i]);
            }

            LinesArrived?.Invoke(partnerId);
        }

        [Rpc(SendTo.Owner)]
        private void ReceiveLiveLineRpc(long partnerId, int index, ConversationLine line)
        {
            GetOrCreateContact(partnerId).PutLine(index, line);

            LinesArrived?.Invoke(partnerId);
            LiveLineArrived?.Invoke(partnerId, line);
        }
    }
}
