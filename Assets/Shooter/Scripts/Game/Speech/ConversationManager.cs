using System;
using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.Service)]
    public class ConversationManager : ShooterComponent, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<(long, long), Conversation> conversationsByKey = new();

        private readonly Dictionary<long, List<Conversation>> conversationsByParticipantId = new();

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

        public Message Say(long authorId, long listenerId, string content, bool spoken, bool urgent)
        {
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
            Said?.Invoke(conversation, message);
            return message;
        }
    }
}
