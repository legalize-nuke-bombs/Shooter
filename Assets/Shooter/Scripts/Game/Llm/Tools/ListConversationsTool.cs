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

        private ConversationManager conversations;
        private Character ownCharacter;

        public override string Name => "list_conversations";

        public override string Description =>
            @"
Use this tool to list your conversations.
To see the content, use read_conversation.
";

        protected override void OnStart()
        {
            conversations = Registers.Of<ConversationManager>(RegisterInactivePolicy.Include).Single();
            ownCharacter = Self.GetComponent<Character>();
            if (ownCharacter == null)
            {
                Log.Error($"Entity {Self.name} failed to get component character required by tool {Name}");
            }
        }

        protected override string Execute(ListConversationsArguments arguments, LlmCallContext context)
        {
            List<Conversation> ownConversations = conversations.Of(ownCharacter.Id);
            if (ownConversations.Count == 0)
            {
                return "Nothing yet";
            }

            var partners = new HashSet<long>();
            foreach (Conversation conversation in ownConversations)
            {
                partners.Add(conversation.Partner(ownCharacter.Id));
            }

            var partnerNames = new Dictionary<long, string>();
            Character.ForEach(character =>
            {
                if (partners.Contains(character.Id))
                {
                    string name = character.GetComponent<Nameable>()?.PromptName() ?? "Unknown";
                    partnerNames[character.Id] = name;
                }
            }, RegisterInactivePolicy.Include
                );

            var sortedConversations = ownConversations
                .OrderByDescending(c => c.Messages.LastOrDefault()?.Time ?? DateTime.MinValue)
                .ToList();

            var sb = new StringBuilder();
            foreach (Conversation conversation in sortedConversations)
            {
                long partner = conversation.Partner(ownCharacter.Id);
                string partnerName = partnerNames.TryGetValue(partner, out string known) ? known : "Unknown";
                Message last = conversation.Messages.LastOrDefault();
                string lastAt = last == null ? "never" : last.Time.ToString(Clock.StampFormat, CultureInfo.InvariantCulture);
                sb.AppendLine($"ID {partner} ({partnerName}) | {conversation.Messages.Count} messages, last message at {lastAt}");
            }
            return sb.ToString();
        }
    }
}
