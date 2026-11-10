using System;
using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.Service)]
    public class ConversationManager : MonoBehaviour, ISaveableComponent
    {
        public const float TalkReach = 8f;
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<(long, long), Conversation> conversationsByKey = new();

        private readonly Dictionary<long, List<Conversation>> conversationsByParticipantId = new();

        private readonly Queue<(Conversation Conversation, Message Message)> pendingSaid = new();

        private bool raisingSaid;

        private Clock clock;

        private struct SaveData
        {
            public List<Conversation> Conversations { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData
            {
                Conversations = conversationsByKey.Values.ToList()
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData saveData = content.ToObject<SaveData>();

            conversationsByKey.Clear();
            conversationsByParticipantId.Clear();

            foreach (Conversation conversation in saveData.Conversations)
            {
                conversationsByKey[conversation.Key] = conversation;
                IndexByParticipantIds(conversation);
            }

            Log.Info($"World remembers {conversationsByKey.Count} conversations");
        }

        public event Action<Conversation, Message> Said;

        private void Start()
        {
            clock = Registers.Instance.Get<Clock>();
        }

        public Conversation GetOrCreate(long firstId, long secondId)
        {
            (long, long) key = Conversation.CreateKey(firstId, secondId);
            if (conversationsByKey.TryGetValue(key, out Conversation conversation)) return conversation;

            conversation = new Conversation(firstId, secondId);
            conversationsByKey.Add(key, conversation);
            IndexByParticipantIds(conversation);
            Log.Info($"Characters {key.Item1} and {key.Item2} start a conversation");
            return conversation;
        }

        public Conversation GetIfPresent(long firstId, long secondId)
        {
            (long, long) key = Conversation.CreateKey(firstId, secondId);
            return conversationsByKey.GetValueOrDefault(key, null);
        }

        public List<Conversation> GetConversationsOf(long characterId)
        {
            return conversationsByParticipantId.TryGetValue(characterId, out List<Conversation> conversations) ? conversations.ToList() : new List<Conversation>();
        }

        private void IndexByParticipantIds(Conversation conversation)
        {
            conversationsByParticipantId.TryAdd(conversation.First, new List<Conversation>());
            conversationsByParticipantId.TryAdd(conversation.Second, new List<Conversation>());
            conversationsByParticipantId[conversation.First].Add(conversation);
            conversationsByParticipantId[conversation.Second].Add(conversation);
        }

        public bool TrySay(long authorId, long listenerId, string content, bool spoken, bool urgent)
        {
            Character listener = Character.FindById(listenerId, RegisterInactivePolicy.Include);
            if (listener != null && !CanHear(listener))
            {
                Log.Info($"Character {authorId} said nothing to {listenerId}: the listener is optimized, too far to hear");
                return false;
            }

            Conversation conversation = GetOrCreate(authorId, listenerId);
            Message message = new()
            {
                AuthorId = authorId,
                Content = content,
                Spoken = spoken,
                Urgent = urgent,
                Time = clock.Now
            };

            conversation.AddMessage(message);
            RaiseSaidInOrder(conversation, message);
            return true;
        }

        public static bool CanHear(Character listener)
        {
            return ShooterEntity.GetComponentStatus(listener) != ShooterEntity.ComponentStatus.Optimized;
        }

        // A listener may answer inside Said: its line waits until every listener has heard the line it answers
        private void RaiseSaidInOrder(Conversation conversation, Message message)
        {
            pendingSaid.Enqueue((conversation, message));
            if (raisingSaid) return;

            raisingSaid = true;
            try
            {
                while (pendingSaid.TryDequeue(out (Conversation Conversation, Message Message) said))
                {
                    Said?.Invoke(said.Conversation, said.Message);
                }
            }
            finally
            {
                raisingSaid = false;
            }
        }
    }
}
