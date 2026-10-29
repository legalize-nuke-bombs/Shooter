using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public sealed class LlmHistory : ShooterComponent, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private int messageLimit = 10000;
        [SerializeField] private int maxSize = 360000;
        [SerializeField] private int hardSize = 460000;

        private readonly List<LlmMessage> messages = new();

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
                // Reasoning is bound to everything before it, and that is forgotten now
                message.Parts.RemoveAll(part => part is LlmReasoningPart);
                AppendMessage(message);
            }
        }

        private void TruncateIfTooLong(LlmMessage message)
        {
            // The model's own reply goes back exactly as it came
            if (message.Role == LlmRole.Assistant) return;

            foreach (LlmPart part in message.Parts)
            {
                switch (part)
                {
                    case LlmTextPart text:
                        text.Text = Truncate(text.Text);
                        break;
                    case LlmToolResultPart toolResult:
                        toolResult.Text = Truncate(toolResult.Text);
                        break;
                }
            }
        }

        private string Truncate(string text)
        {
            if (text.Length <= messageLimit) return text;

            Log.Warn($"Entity {name} takes a {text.Length} character message, cut to {messageLimit}");
            string truncationMarker = $"\n[output truncated, original length {text.Length} characters]";
            return text.Substring(0, messageLimit - truncationMarker.Length) + truncationMarker;
        }

        private static int MeasureSize(LlmMessage message)
        {
            return message.Parts.Sum(part => part.Length + 20) + 20;
        }
    }
}
