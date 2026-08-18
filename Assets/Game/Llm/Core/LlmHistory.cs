using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Shooter.Game.Llm
{
    public sealed class LlmHistory : MonoBehaviour
    {
        [SerializeField] private int maxSize = 30000;

        private readonly List<LlmMessage> messages = new();
        private int snapshot;

        public int Count => messages.Count;
        public int Size { get; private set; }
        public bool Unseen { get; private set; }
        public bool Overflowing => Size >= maxSize;
        public IReadOnlyList<LlmMessage> Messages => messages;

        public void Append(LlmMessage message)
        {
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

        public void Snapshot()
        {
            snapshot = messages.Count;
        }

        public void Forget()
        {
            var fresh = messages.Skip(snapshot).ToList();

            messages.Clear();
            Size = 0;

            foreach (LlmMessage message in fresh) Append(message);

            snapshot = messages.Count;
        }

        private static int Sized(LlmMessage message)
        {
            int size = (message.Content?.Length ?? 0) + 20;

            if (message.ToolCalls != null)
                foreach (LlmToolCall call in message.ToolCalls)
                    size += (call.Name?.Length ?? 0) + (call.Arguments?.Length ?? 0) + 20;

            return size;
        }
    }
}
