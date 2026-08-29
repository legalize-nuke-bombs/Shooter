using System;
using Shooter.Game.Llm.Notes;
using UnityEngine;

namespace Shooter.Game.Llm.SearchNotes
{
    [RequireComponent(typeof(LlmNotes))]
    public class SearchNotesTool : LlmTool<SearchNotesArguments>
    {
        private LlmNotes notes;

        public override string Name => "search_notes";

        public override string Description =>
            @"
Use this tool to find which notes mention something: searches all notes with a regex pattern and returns match counts per note.
";

        protected override void Awake()
        {
            base.Awake();
            notes = GetComponent<LlmNotes>();
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
