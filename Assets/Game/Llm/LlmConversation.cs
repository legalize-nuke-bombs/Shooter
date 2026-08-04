using System;
using System.Collections.Generic;

namespace Shooter.Game.Llm
{
    public class LlmConversation
    {
        private readonly List<LlmMessage> messages = new List<LlmMessage>();
        private Action<string> onAnswer = null;

        public bool Pending()
        {
            return messages.Count > 0 && messages[^1].Role == LlmRole.User;
        }

        public void RegisterUserMessage(LlmMessage message, Action<string> onAnswerEvent)
        {
            if (message.Role != LlmRole.User)
            {
                throw new ArgumentException("invalid role");
            }
            messages.Add(message);
            onAnswer = onAnswerEvent;
        }

        public void RegisterModelMessage(LlmMessage message)
        {
            if (message.Role != LlmRole.Model)
            {
                throw new ArgumentException("invalid role");
            }

            messages.Add(message);

            var callback = onAnswer;
            onAnswer = null;

            if (callback != null)
            {
                callback.Invoke(message.Content);
            }
        }
    }
}
