using System;
using Shooter.Game.Llm.Notes;
using Shooter.Logging;

namespace Shooter.Game.Llm.SearchNotes
{
    [Serializable]
    public class SearchNotesTool : LlmTool<SearchNotesArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private LlmNotes notes;

        public override string Name => "search_notes";

        public override string Description =>
            @"
Use this tool to find which notes mention something: searches all notes with a regex pattern and returns match counts per note.
";

        protected override void OnStart()
        {
            notes = Self.GetComponent<LlmNotes>();
            if (notes == null)
            {
                Log.Error($"Entity {Self.name} does not have LlmNotes component required by tool {Name}");
            }
        }

        protected override string Execute(SearchNotesArguments arguments, LlmCallContext context)
        {
            string result;
            try
            {
                result = notes.Matches(arguments.Pattern);
            }
            catch (Exception e)
            {
                result = $"Failed to search: {e.Message}";
            }

            return result;
        }
    }
}
