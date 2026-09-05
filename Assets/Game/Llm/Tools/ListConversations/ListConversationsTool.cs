using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Speech;
using Shooter.Logging;

namespace Shooter.Game.Llm.ListConversations
{
    [Serializable]
    public class ListConversationsTool : LlmTool<ListConversationsArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private Character ownCharacter;

        public override string Name => "list_conversations";

        public override string Description =>
            @"
Use this tool to list your conversations.
To see the content, use read_conversation.
";

        protected override void OnStart()
        {
            ownCharacter = Self.GetComponent<Character>();
            if (ownCharacter == null)
            {
                Log.Error($"Entity {Self.name} failed to get component character required by tool {Name}");
            }
        }

        protected override string Execute(ListConversationsArguments arguments, LlmCallContext context)
        {
            List<Conversation> conversations = ConversationManager.Current.Of(ownCharacter.Id);
            if (conversations.Count == 0)
            {
                return "Nothing yet";
            }

            var partners = new HashSet<long>();
            foreach (Conversation conversation in conversations)
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
            }, Inactive.Include
                );

            var sortedConversations = conversations
                .OrderByDescending(c => c.Messages.LastOrDefault()?.Time ?? DateTime.MinValue)
                .ToList();

            var sb = new StringBuilder();
            foreach (Conversation conversation in sortedConversations)
            {
                long partner = conversation.Partner(ownCharacter.Id);
                sb.AppendLine($"ID {partner} ({partnerNames[partner]}) | {conversation.Messages.Count} messages, last message at {conversation.Messages.LastOrDefault()?.Time ?? DateTime.MinValue}");
            }
            return sb.ToString();
        }
    }
}
