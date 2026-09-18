using System;
using System.Collections;
using System.Collections.Generic;
using Shooter.Game.Core;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Speech
{
    [RequireComponent(typeof(Character))]
    public class PlayerConversations : NetworkBehaviour
    {
        private const int BatchBytes = 16 * 1024;
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<long, Contact> contacts = new();

        private Character character;
        private ConversationManager conversations;
        private Coroutine snapshot;
        private bool synced;

        public long CharacterId => character.Id;

        public IReadOnlyDictionary<long, Contact> Contacts => contacts;

        public event Action<long> Arrived;

        // A line said live, not brought by the snapshot
        public event Action<long, Line> Heard;

        public event Action Cleared;

        private void Awake()
        {
            character = GetComponent<Character>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            conversations = GameState.Get<ConversationManager>();
            conversations.Said += Relay;
        }

        public override void OnNetworkDespawn()
        {
            if (conversations != null)
            {
                conversations.Said -= Relay;
                conversations = null;
            }

            if (snapshot != null)
            {
                StopCoroutine(snapshot);
                snapshot = null;
            }

            Clear();
        }

        public override void OnGainedOwnership()
        {
            Clear();
        }

        public override void OnLostOwnership()
        {
            Clear();
        }

        private void Update()
        {
            // The owner pulls its history itself: one path for a fresh body, a reclaimed one and the host,
            // whose own body never changes owner and so never raises an ownership event
            if (!IsOwner || synced) return;

            synced = true;
            RequestRpc();
        }

        public Contact ContactOf(long partnerId)
        {
            if (!contacts.TryGetValue(partnerId, out Contact contact))
            {
                contact = new Contact(partnerId);
                contacts.Add(partnerId, contact);
            }

            return contact;
        }

        private void Clear()
        {
            contacts.Clear();
            synced = false;
            Cleared?.Invoke();
        }

        private void Relay(Conversation conversation, Message message)
        {
            if (conversation.First != CharacterId && conversation.Second != CharacterId) return;

            // An offline body is owned by the host; the line comes with the snapshot when its owner returns
            if (!gameObject.activeInHierarchy) return;

            LineRpc(conversation.Partner(CharacterId), conversation.IndexOf(message), Line.Of(message));
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void RequestRpc()
        {
            Log.Info($"Player {OwnerClientId} asks for its conversations");

            if (snapshot != null) StopCoroutine(snapshot);
            snapshot = StartCoroutine(Snapshot());
        }

        // One batch per frame: an overflowing reliable queue drops the connection
        private IEnumerator Snapshot()
        {
            int sent = 0;
            foreach (Conversation pair in conversations.Of(CharacterId))
            {
                long partner = pair.Partner(CharacterId);
                IReadOnlyList<Message> messages = pair.Messages;
                int count = messages.Count;

                var batch = new List<Line>();
                int first = 0;
                int bytes = 0;
                for (int i = 0; i < count; i++)
                {
                    Line line = Line.Of(messages[i]);
                    batch.Add(line);
                    bytes += 32 + 2 * (line.Content?.Length ?? 0);

                    if (bytes < BatchBytes && i < count - 1) continue;

                    LinesRpc(partner, first, batch.ToArray());
                    sent += batch.Count;
                    first = i + 1;
                    batch.Clear();
                    bytes = 0;
                    yield return null;
                }
            }

            Log.Info($"Player {OwnerClientId} received its conversations: {sent} lines");
            snapshot = null;
        }

        [Rpc(SendTo.Owner)]
        private void LinesRpc(long partnerId, int first, Line[] lines)
        {
            Contact contact = ContactOf(partnerId);
            for (int i = 0; i < lines.Length; i++) contact.Put(first + i, lines[i]);

            Arrived?.Invoke(partnerId);
        }

        [Rpc(SendTo.Owner)]
        private void LineRpc(long partnerId, int index, Line line)
        {
            ContactOf(partnerId).Put(index, line);

            Arrived?.Invoke(partnerId);
            Heard?.Invoke(partnerId, line);
        }
    }
}
