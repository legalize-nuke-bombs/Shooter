using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [Serializable]
    public class ListConversationsTool : LlmTool<ListConversationsArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private ConversationManager conversationManager;
        private Character ownCharacter;

        public override string Name => "list_conversations";

        public override string Description =>
            @"
Use this tool to list your conversations.
To see the content, use read_conversation.
";

        protected override void Start()
        {
            conversationManager = Registers.Get<ConversationManager>();
            ownCharacter = Self.GetComponent<Character>();
            if (ownCharacter == null)
            {
                Log.Error($"Entity {Self.name} failed to get component character required by tool {Name}");
            }
        }

        protected override string Execute(ListConversationsArguments arguments, LlmCallContext context)
        {
            List<Conversation> conversations = conversationManager.GetConversationsOf(ownCharacter.Id);
            if (conversations.Count == 0)
            {
                return "Nothing yet";
            }

            var partnerIds = new HashSet<long>();
            foreach (Conversation conversation in conversations)
            {
                partnerIds.Add(conversation.GetPartnerId(ownCharacter.Id));
            }

            var partnerNames = new Dictionary<long, string>();
            Character.ForEach(character =>
            {
                if (partnerIds.Contains(character.Id))
                {
                    string promptName = character.GetComponent<Nameable>()?.GetPromptName() ?? "Unknown";
                    partnerNames[character.Id] = promptName;
                }
            }, RegisterInactivePolicy.Include);

            var sortedConversations = conversations.OrderByDescending(conversation => conversation.Messages.LastOrDefault()?.Time ?? DateTime.MinValue).ToList();

            StringBuilder result = new();
            foreach (Conversation conversation in sortedConversations)
            {
                long partnerId = conversation.GetPartnerId(ownCharacter.Id);
                string partnerName = partnerNames.TryGetValue(partnerId, out string knownName) ? knownName : "Unknown";
                Message lastMessage = conversation.Messages.LastOrDefault();
                string lastMessageTime = lastMessage == null ? "never" : lastMessage.Time.ToString(Clock.StampFormat, CultureInfo.InvariantCulture);
                result.AppendLine($"ID {partnerId} ({partnerName}) | {conversation.Messages.Count} messages, last message at {lastMessageTime}");
            }
            return result.ToString();
        }
    }
}
