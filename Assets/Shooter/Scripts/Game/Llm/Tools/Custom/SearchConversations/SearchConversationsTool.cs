using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [Serializable]
    public class SearchConversationsTool : LlmTool<SearchConversationsArguments>
    {
        private const int ShownMatchLimit = 20;
        private static readonly Journal Log = Logs.Here();

        private ConversationManager conversationManager;
        private Character ownCharacter;

        public override string Name => "search_conversations";

        public override string Description =>
            @"
Use this tool to search your conversations for messages matching a regex pattern (case-insensitive).
Pass `target_id` of a character to search only your conversation with them, or -1 to search all your conversations.
Every line starts with the message index: pass it as `from` to read_conversation to read what was said around it.
Only the newest matches are shown, narrow the pattern if there are more.
";

        protected override void Start()
        {
            conversationManager = Registers.Get<ConversationManager>();
            ownCharacter = Self.GetComponent<Character>();
            if (ownCharacter == null)
            {
                Log.Error($"Entity {Self.name} does not have character component required by tool {Name}");
            }
        }

        protected override string Execute(SearchConversationsArguments arguments, LlmCallContext context)
        {
            if (string.IsNullOrEmpty(arguments.Pattern))
            {
                return "`pattern` is empty";
            }
            if (arguments.TargetId == ownCharacter.Id)
            {
                return $"The specified ID ({arguments.TargetId}) belongs to you";
            }

            List<Conversation> searchedConversations;
            if (arguments.TargetId < 0)
            {
                searchedConversations = conversationManager.GetConversationsOf(ownCharacter.Id);
            }
            else
            {
                Conversation conversation = conversationManager.GetIfPresent(ownCharacter.Id, arguments.TargetId);
                if (conversation == null)
                {
                    return $"You don't have conversation with ID {arguments.TargetId}";
                }
                searchedConversations = new List<Conversation> { conversation };
            }

            Regex regex = new(arguments.Pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
            var hits = new List<(Conversation conversation, int index)>();
            var matchCountsByConversation = new Dictionary<Conversation, int>();
            foreach (Conversation conversation in searchedConversations)
            {
                List<int> matchingIndexes = conversation.FindMatchingMessageIndexes(regex);
                if (matchingIndexes.Count == 0) continue;

                matchCountsByConversation[conversation] = matchingIndexes.Count;
                foreach (int index in matchingIndexes)
                {
                    hits.Add((conversation, index));
                }
            }
            if (hits.Count == 0)
            {
                return "No matches";
            }

            var shownHits = hits
                .OrderByDescending(hit => hit.conversation.Messages[hit.index].Time)
                .Take(ShownMatchLimit)
                .ToList();

            var partnerIds = new HashSet<long>();
            foreach ((Conversation conversation, int _) in shownHits)
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

            StringBuilder result = new();
            foreach (IGrouping<Conversation, (Conversation conversation, int index)> hitsOfConversation in shownHits.GroupBy(hit => hit.conversation))
            {
                long partnerId = hitsOfConversation.Key.GetPartnerId(ownCharacter.Id);
                string partnerName = partnerNames.TryGetValue(partnerId, out string knownName) ? knownName : "Unknown";
                result.AppendLine($"Conversation with ID {partnerId} ({partnerName}), {matchCountsByConversation[hitsOfConversation.Key]} matches:");
                foreach ((Conversation conversation, int index) in hitsOfConversation)
                {
                    Message message = conversation.Messages[index];
                    string stamp = message.Time.ToString(Clock.StampFormat, CultureInfo.InvariantCulture);
                    result.AppendLine($"#{index} [{stamp}]{(message.Spoken ? "" : " (radio)")} ID {message.AuthorId}: {message.Content}");
                }
            }
            if (hits.Count > shownHits.Count)
            {
                result.AppendLine($"Showing the {shownHits.Count} newest of {hits.Count} matches, narrow the pattern or pass target_id.");
            }
            return result.ToString();
        }
    }
}
