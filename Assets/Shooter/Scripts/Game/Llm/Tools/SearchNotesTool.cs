using System;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [Serializable]
    public class SearchNotesTool : LlmTool<SearchNotesArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private LlmNotes llmNotes;

        public override string Name => "search_notes";

        public override string Description =>
            @"
Use this tool to find which notes mention something: searches all notes with a regex pattern and returns match counts per note.
";

        protected override void HandleAttached()
        {
            llmNotes = Self.GetComponent<LlmNotes>();
            if (llmNotes == null)
            {
                Log.Error($"Entity {Self.name} does not have LlmNotes component required by tool {Name}");
            }
        }

        protected override string Execute(SearchNotesArguments arguments, LlmCallContext context)
        {
            string result;
            try
            {
                result = llmNotes.SearchNotes(arguments.Pattern);
            }
            catch (Exception e)
            {
                result = $"Failed to search: {e.Message}";
            }

            return result;
        }
    }
}
