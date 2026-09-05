using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Shooter.Game.Core;
using Shooter.Game.Core.Saves;
using Shooter.Game.World;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Speech
{
    [DefaultExecutionOrder(ExecutionOrder.Service)]
    public class ConversationManager : MonoBehaviour, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<(long, long), Conversation> conversations = new();

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
            foreach (Conversation conversation in sd.Conversations)
                conversations[conversation.Key] = conversation;

            Log.Info($"World remembers {conversations.Count} conversations");
        }

        public static ConversationManager Current { get; private set; }

        public event Action<Conversation, Message> Said;

        private void Awake()
        {
            if (Current != null)
            {
                Log.Error("Singleton class has more than one instance");
            }
            Current = this;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        public Conversation Between(long first, long second)
        {
            (long, long) pair = Conversation.Pair(first, second);
            if (conversations.TryGetValue(pair, out Conversation conversation)) return conversation;

            conversation = new Conversation(first, second);
            conversations.Add(pair, conversation);
            Log.Info($"Characters {pair.Item1} and {pair.Item2} start a conversation");
            return conversation;
        }

        public Message Say(long authorId, long listenerId, string content, bool spoken)
        {
            Conversation conversation = Between(authorId, listenerId);
            var message = new Message
            {
                AuthorId = authorId,
                Content = content,
                Spoken = spoken,
                Time = Clock.Current.Now.ToString(Message.TimeFormat, CultureInfo.InvariantCulture)
            };

            conversation.Add(message);
            Said?.Invoke(conversation, message);
            return message;
        }
    }
}
