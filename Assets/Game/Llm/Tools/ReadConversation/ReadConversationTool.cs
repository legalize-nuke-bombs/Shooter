using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Shooter.Game.Core;
using Shooter.Game.Speech;
using Shooter.Game.World;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm.ReadConversation
{
    [Serializable]
    public class ReadConversationTool : LlmTool<ReadConversationArguments>
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private int maxOutput = 10000;

        private Character ownCharacter;
        public override string Name => "read_conversation";

        public override string Description =>
            @"
Use this tool to read your conversation with specified character by their id.
This tool uses pagination.
Specify `from` (the message number from which you want to retrieve the conversation history, starts from 0) and `size` (the number of messages you want to retrieve).
";

        protected override void OnStart()
        {
            ownCharacter = Self.GetComponent<Character>();
            if (ownCharacter == null)
            {
                Log.Error($"Entity {Self.name} failed to get component character required by tool {Name}");
            }
        }

        protected override string Execute(ReadConversationArguments arguments, LlmCallContext context)
        {
            long targetId = arguments.TargetId;
            int from = arguments.From;
            int size = arguments.Size;

            if (from < 0)
            {
                return "`from` can not be negative";
            }
            if (size <= 0)
            {
                return "`size` must be positive";
            }
            if (ownCharacter.Id == targetId)
            {
                return $"The specified ID ({targetId}) belongs to you";
            }

            Conversation conversation = ConversationManager.Current.GetIfPresent(ownCharacter.Id, targetId);
            if (conversation == null)
            {
                return $"You don't have conversation with ID {targetId}";
            }

            IReadOnlyList<Message> messages = conversation.Messages;
            if (messages.Count == 0)
            {
                return $"The conversation with ID {targetId} is empty";
            }
            if (from >= messages.Count)
            {
                return $"Invalid `from`: specified conversation has {messages.Count} messages, `from` is {from}";
            }

            var sb = new StringBuilder();
            for (int i = from; i < Math.Min(messages.Count, from + size); i++)
            {
                Message message = messages[i];
                string stamp = message.Time.ToString(Clock.StampFormat, CultureInfo.InvariantCulture);
                sb.AppendLine($"[{stamp}]{(message.Spoken ? "" : " (radio)")} ID {message.AuthorId}: {message.Content}");
            }

            if (sb.Length > maxOutput)
            {
                return $"Result is too long {sb.Length} ch (max is {maxOutput} ch), consider invoking this tool with a smaller page size.";
            }

            return sb.ToString();
        }
    }
}
