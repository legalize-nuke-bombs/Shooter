using System;
using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DefaultExecutionOrder(ExecutionOrder.Service)]
    public class ConversationManager : ShooterBehaviour, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<(long, long), Conversation> conversations = new();

        private readonly Dictionary<long, List<Conversation>> conversationsById = new();

        private Clock clock;

        public string ComponentKey => "ConversationManager";
        private struct SaveData
        {
            public List<Conversation> Conversations { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData
            {
                Conversations = conversations.Values.ToList()
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.To<SaveData>();

            conversations.Clear();
            conversationsById.Clear();

            foreach (Conversation conversation in sd.Conversations)
            {
                conversations[conversation.Key] = conversation;
                UpdateIndex(conversation);
            }

            Log.Info($"World remembers {conversations.Count} conversations");
        }

        public event Action<Conversation, Message> Said;

        private void Start()
        {
            clock = Registers.Of<Clock>(RegisterInactivePolicy.Include).Single();
        }

        public Conversation GetOrCreate(long first, long second)
        {
            (long, long) pair = Conversation.Pair(first, second);
            if (conversations.TryGetValue(pair, out Conversation conversation)) return conversation;

            conversation = new Conversation(first, second);
            conversations.Add(pair, conversation);
            UpdateIndex(conversation);
            Log.Info($"Characters {pair.Item1} and {pair.Item2} start a conversation");
            return conversation;
        }

        public Conversation GetIfPresent(long first, long second)
        {
            (long, long) pair = Conversation.Pair(first, second);
            return conversations.GetValueOrDefault(pair, null);
        }

        public List<Conversation> Of(long id)
        {
            return conversationsById.TryGetValue(id, out List<Conversation> found) ? found.ToList() : new List<Conversation>();
        }

        private void UpdateIndex(Conversation conversation)
        {
            conversationsById.TryAdd(conversation.First, new List<Conversation>());
            conversationsById.TryAdd(conversation.Second, new List<Conversation>());
            conversationsById[conversation.First].Add(conversation);
            conversationsById[conversation.Second].Add(conversation);
        }

        public Message Say(long authorId, long listenerId, string content, bool spoken, bool urgent)
        {
            Conversation conversation = GetOrCreate(authorId, listenerId);
            var message = new Message
            {
                AuthorId = authorId,
                Content = content,
                Spoken = spoken,
                Urgent = urgent,
                Time = clock.Now
            };

            conversation.Add(message);
            Said?.Invoke(conversation, message);
            return message;
        }
    }
}
