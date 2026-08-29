using System;
using Shooter.Game.Llm.Notes;
using UnityEngine;

namespace Shooter.Game.Llm.AddNote
{
    [RequireComponent(typeof(LlmNotes))]
    public class AddNoteTool : LlmTool<AddNoteArguments>
    {
        private LlmNotes notes;

        public override string Name => "add_note";

        public override string Description =>
            @$"
Use this tool to add a new note.
The name is a short id: you will retype it exactly to address the note. The description is shown in the notes list: write what is inside and why to open it.
Keep one subject per note. To change an existing note, use update_note.
Max note name size: {notes.NameLimit}
Max note description size: {notes.DescriptionLimit}
Max note content size: {notes.ContentLimit}
Max notes number: {notes.AmountLimit}
";

        protected override void Awake()
        {
            base.Awake();
            notes = GetComponent<LlmNotes>();
        }

        protected override string Execute(AddNoteArguments arguments, LlmCallContext context)
        {
            string result;
            try
            {
                notes.Add(
                    arguments.Name,
                    new LlmNote
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
