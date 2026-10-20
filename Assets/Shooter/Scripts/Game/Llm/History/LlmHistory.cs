using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public sealed class LlmHistory : MonoBehaviour, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private int messageLimit = 10000;
        [SerializeField] private int maxSize = 100000;
        [SerializeField] private int hardSize = 200000;

        private readonly List<LlmMessage> messages = new();

        public string ComponentKey => "LlmHistory";
        private struct SaveData
        {
            public List<LlmMessage> Messages { get; set; }
            public bool Unseen { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData
            {
                Messages = new List<LlmMessage>(messages),
                Unseen = HasUnseenUrgentEvent
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData saveData = content.ToObject<SaveData>();
            messages.Clear();
            Size = 0;
            HasUnseenUrgentEvent = saveData.Unseen;
            foreach (LlmMessage message in saveData.Messages)
            {
                AppendMessage(message);
            }
        }

        public int Count => messages.Count;
        public int Size { get; private set; }
        public bool HasUnseenUrgentEvent { get; private set; }
        public bool IsOverflowing => Size >= maxSize;
        public bool IsBursting => Size >= hardSize;
        public IReadOnlyList<LlmMessage> Messages => messages;

        public void AppendMessage(LlmMessage message)
        {
            TruncateIfTooLong(message);
            messages.Add(message);
            Size += MeasureSize(message);
        }

        public void AppendEvent(LlmMessage message, bool isUrgent)
        {
            AppendMessage(message);
            HasUnseenUrgentEvent = HasUnseenUrgentEvent || isUrgent;
        }

        public void MarkEventsSeen()
        {
            HasUnseenUrgentEvent = false;
        }

        public int FindLastAssistantMessageIndex()
        {
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                if (messages[i].Role == LlmRole.Assistant) return i;
            }

            return 0;
        }

        public void ForgetMessagesBefore(int firstKeptIndex)
        {
            var keptMessages = messages.Skip(firstKeptIndex).ToList();

            messages.Clear();
            Size = 0;

            foreach (LlmMessage message in keptMessages)
            {
                AppendMessage(message);
            }
        }

        private void TruncateIfTooLong(LlmMessage message)
        {
            if (message.Content == null) return;

            int contentLength = message.Content.Length;
            if (contentLength <= messageLimit) return;

            Log.Warn($"Entity {name} takes a {contentLength} character message, cut to {messageLimit}");
            string truncationMarker = $"\n[output truncated, original length {contentLength} characters]";
            message.Content = message.Content.Substring(0, messageLimit - truncationMarker.Length) + truncationMarker;
        }

        private static int MeasureSize(LlmMessage message)
        {
            int size = (message.Content == null ? 0 : message.Content.Length) + 20;

            if (message.ToolCalls != null)
            {
                foreach (LlmToolCall toolCall in message.ToolCalls)
                {
                    size += (toolCall.Name?.Length ?? 0) + (toolCall.Arguments?.Length ?? 0) + 20;
                }
            }

            return size;
        }
    }
}
