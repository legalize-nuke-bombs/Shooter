using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Speech;
using Shooter.Game.World;
using Shooter.Logging;

namespace Shooter.Game.Llm.SearchConversations
{
    [Serializable]
    public class SearchConversationsTool : LlmTool<SearchConversationsArguments>
    {
        private const int MatchLimit = 20;
        private static readonly Journal Log = Logs.Here();

        private Character ownCharacter;

        public override string Name => "search_conversations";

        public override string Description =>
            @"
Use this tool to search your conversations for messages matching a regex pattern (case-insensitive).
Pass `target_id` of a character to search only your conversation with them, or -1 to search all your conversations.
Every line starts with the message index: pass it as `from` to read_conversation to read what was said around it.
Only the newest matches are shown, narrow the pattern if there are more.
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
            if (string.IsNullOrEmpty(arguments.Pattern))
            {
                return "`pattern` is empty";
            }
            if (arguments.TargetId == ownCharacter.Id)
            {
                return $"The specified ID ({arguments.TargetId}) belongs to you";
            }

            ConversationManager conversations = ConversationManager.Current;
            List<Conversation> searched;
            if (arguments.TargetId < 0)
            {
                searched = conversations.Of(ownCharacter.Id);
            }
            else
            {
                Conversation conversation = conversations.GetIfPresent(ownCharacter.Id, arguments.TargetId);
                if (conversation == null)
                {
                    return $"You don't have conversation with ID {arguments.TargetId}";
                }
                searched = new List<Conversation> { conversation };
            }

            var regex = new Regex(arguments.Pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
            var hits = new List<(Conversation conversation, int index)>();
            var totals = new Dictionary<Conversation, int>();
            foreach (Conversation conversation in searched)
            {
                List<int> indexes = conversation.Matches(regex);
                if (indexes.Count == 0) continue;

                totals[conversation] = indexes.Count;
                foreach (int index in indexes)
                {
                    hits.Add((conversation, index));
                }
            }
            if (hits.Count == 0)
            {
                return "No matches";
            }

            var shown = hits
                .OrderByDescending(hit => hit.conversation.Messages[hit.index].Time)
                .Take(MatchLimit)
                .ToList();

            var partners = new HashSet<long>();
            foreach ((Conversation conversation, int _) in shown)
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
            }, Inactive.Include);

            var sb = new StringBuilder();
            foreach (IGrouping<Conversation, (Conversation conversation, int index)> group in shown.GroupBy(hit => hit.conversation))
            {
                long partner = group.Key.Partner(ownCharacter.Id);
                string partnerName = partnerNames.TryGetValue(partner, out string known) ? known : "Unknown";
                sb.AppendLine($"Conversation with ID {partner} ({partnerName}), {totals[group.Key]} matches:");
                foreach ((Conversation conversation, int index) in group)
                {
                    Message message = conversation.Messages[index];
                    string stamp = message.Time.ToString(Clock.StampFormat, CultureInfo.InvariantCulture);
                    sb.AppendLine($"#{index} [{stamp}]{(message.Spoken ? "" : " (radio)")} ID {message.AuthorId}: {message.Content}");
                }
            }
            if (hits.Count > shown.Count)
            {
                sb.AppendLine($"Showing the {shown.Count} newest of {hits.Count} matches, narrow the pattern or pass target_id.");
            }
            return sb.ToString();
        }
    }
}
