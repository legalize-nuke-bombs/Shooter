using System;
using Shooter.Game.Llm.Notes;
using UnityEngine;

namespace Shooter.Game.Llm.ReadNotes
{
    [RequireComponent(typeof(LlmNotes))]
    public class ReadNotesTool : LlmTool<ReadNotesArguments>
    {
        private LlmNotes notes;

        protected override void Awake()
        {
            base.Awake();
            notes = GetComponent<LlmNotes>();
        }

        public override string Name => "read_notes";

        public override string Description =>
            @"
Use this tool to read the notes.
Don't pass anything to get the list of notes.
Pass a `searchPattern` to find matches within the note content based on a regex pattern.
Pass `noteName` to read the note's content by its name.
";

        protected override string Execute(ReadNotesArguments arguments)
        {
            string searchPattern = arguments.SearchPattern;
            string noteName = arguments.NoteName;

            if (!String.IsNullOrEmpty(searchPattern))
            {
                return Search(searchPattern);
            }
            if (!String.IsNullOrEmpty(noteName))
            {
                return Read(noteName);
            }
            return NotesList();
        }

        private string Search(string searchPattern)
        {
            string result;
            try
            {
                result = notes.Matches(searchPattern);
            }
            catch (Exception e)
            {
                result = $"Failed to search: {e.Message}";
            }
            return result;
        }

        private string Read(string noteName)
        {
            string result;
            try
            {
                result = notes.Read(noteName);
            }
            catch (Exception e)
            {
                result = $"Failed to read: {e.Message}";
            }
            return result;
        }

        private string NotesList()
        {
            return notes.List();
        }
    }
}
