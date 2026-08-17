using System;
using Shooter.Game.Llm.Notes;
using UnityEngine;

namespace Shooter.Game.Llm.AddNote
{
    [RequireComponent(typeof(LlmNotes))]
    public class AddNoteTool : LlmTool<AddNoteArguments>
    {
        private LlmNotes notes;

        protected override void Awake()
        {
            base.Awake();
            notes = GetComponent<LlmNotes>();
        }

        public override string Name => "add_note";

        public override string Description =>
            @$"
Use this tool to add a new note.
This tool only adds a note, it doesn't modify it. To modify a note, delete it and add it again.
Max note name size: {notes.NameLimit}
Max note description size: {notes.DescriptionLimit}
Max note content size: {notes.ContentLimit}
Max notes number: {notes.AmountLimit}
";

        protected override string Execute(AddNoteArguments arguments)
        {
            string result;
            try
            {
                notes.Add(
                    arguments.Name,
                    new LlmNote()
                    {
                        Content = arguments.Content,
                        Description = arguments.Description
                    }
                );
                result = "Added";
            }
            catch (Exception e)
            {
                result = $"Failed to add a note: {e.Message}";
            }
            return result;
        }
    }
}
