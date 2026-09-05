using System.Collections.Generic;
using System.Linq;
using Shooter.Game.Core.Saves;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm
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
                Unseen = Unseen
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.To<SaveData>();
            messages.Clear();
            Size = 0;
            Unseen = sd.Unseen;
            foreach (LlmMessage message in sd.Messages) Append(message);
        }

        public int Count => messages.Count;
        public int Size { get; private set; }
        public bool Unseen { get; private set; }
        public bool Overflowing => Size >= maxSize;
        public bool Bursting => Size >= hardSize;
        public IReadOnlyList<LlmMessage> Messages => messages;

        public void Append(LlmMessage message)
        {
            Gate(message);
            messages.Add(message);
            Size += Sized(message);
        }

        public void Arrive(LlmMessage message, bool urgent)
        {
            Append(message);
            Unseen = Unseen || urgent;
        }

        public void Seen()
        {
            Unseen = false;
        }

        public int LastTurn()
        {
            for (int i = messages.Count - 1; i >= 0; i--)
                if (messages[i].Role == LlmRole.Assistant)
                    return i;

            return 0;
        }

        public void Forget(int keepFrom)
        {
            var fresh = messages.Skip(keepFrom).ToList();

            messages.Clear();
            Size = 0;

            foreach (LlmMessage message in fresh) Append(message);
        }

        private void Gate(LlmMessage message)
        {
            if (message.Content == null)
            {
                return;
            }

            int length = message.Content.Length;
            if (length <= messageLimit) return;

            Log.Warn($"Entity {name} takes a {length} character message, cut to {messageLimit}");
            string marker = $"\n[output truncated, original length {length} characters]";
            message.Content = message.Content.Substring(0, messageLimit - marker.Length) + marker;
        }

        private static int Sized(LlmMessage message)
        {
            int size = (message.Content == null ? 0 : message.Content.Length) + 20;

            if (message.ToolCalls != null)
            {
                foreach (LlmToolCall call in message.ToolCalls)
                {
                    size += (call.Name?.Length ?? 0) + (call.Arguments?.Length ?? 0) + 20;
                }
            }

            return size;
        }
    }
}
