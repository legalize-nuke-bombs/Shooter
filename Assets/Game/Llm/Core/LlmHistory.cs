using System.Collections.Generic;
using System.Linq;

namespace Shooter.Game.Llm
{
    public sealed class LlmHistory
    {
        private readonly List<LlmMessage> messages = new List<LlmMessage>();

        public int Count => messages.Count;
        public int Size { get; private set; }
        public bool Unseen { get; private set; }
        public IReadOnlyList<LlmMessage> Messages => messages;

        public void Append(LlmMessage message)
        {
            messages.Add(message);
            Size += Sized(message);
        }

        public void Arrive(LlmMessage message)
        {
            Append(message);
            Unseen = true;
        }

        public void Seen()
        {
            Unseen = false;
        }

        public void Retell(string story, int snapshot)
        {
            List<LlmMessage> fresh = messages.Skip(snapshot).ToList();

            messages.Clear();
            Size = 0;

            Append(new LlmMessage { Role = LlmRole.User, Content = story });
            foreach (LlmMessage message in fresh) Append(message);
        }

        private static int Sized(LlmMessage message)
        {
            int size = (message.Content?.Length ?? 0) + 20;

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
