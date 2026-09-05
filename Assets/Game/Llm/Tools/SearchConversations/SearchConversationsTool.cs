using System;
using System.Collections.Generic;
using System.Text;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Speech;
using Shooter.Logging;

namespace Shooter.Game.Llm.SearchConversations
{
    [Serializable]
    public class SearchConversationsTool : LlmTool<SearchConversationsArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private Character ownCharacter;

        public override string Name => "search_conversations";

        public override string Description =>
            @"
Use this tool to find messages in the chat history that mention something by regexp pattern.
Pass valid `targetId` to search for matches in the dialogue only with the character having that ID.
Pass `targetId` as -1 to search for matches in the dialogue with all characters.
";

        protected override void OnStart()
        {
            ownCharacter = Self.GetComponent<Character>();
            if (ownCharacter == null)
            {
                Log.Error($"Entity {Self.name} does not have character component required by tool {Name}");
            }
        }

        protected override string Execute(SearchConversationsArguments arguments, LlmCallContext context)
        {
            var matches = new Dictionary<long, List<int>>();

            if (arguments.TargetId < 0)
            {
                matches = ConversationManager.Current.Matches(ownCharacter.Id, arguments.Pattern);
            }
            else
            {
                List<int> messageIds = ConversationManager.Current.Matches(ownCharacter.Id, arguments.TargetId, arguments.Pattern);
                if (messageIds.Count > 0) matches.Add(arguments.TargetId, messageIds);
            }

            if (matches.Count == 0)
            {
                return "No matches";
            }

            var partners = new HashSet<long>();
            foreach (long partnerId in matches.Keys)
            {
                partners.Add(partnerId);
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

            var sb = new StringBuilder();
            foreach (var kvp in matches)
            {
                long partnerId = kvp.Key;
                List<int> messageIds = kvp.Value;
                sb.Append($"Conversation with ID {partnerId} ({partnerNames[partnerId]}) | {messageIds.Count} matches: message with indexes ");
                foreach (int messageId in messageIds)
                {
                    sb.Append($"{messageId} ");
                }
            }
            return sb.ToString();
        }
    }
}
