using System;
using Shooter.Game.Llm.Notes;
using UnityEngine;

namespace Shooter.Game.Llm.DeleteNote
{
    [RequireComponent(typeof(LlmNotes))]
    public class DeleteNoteTool : LlmTool<DeleteNoteArguments>
    {
        private LlmNotes notes;

        public override string Name => "delete_note";

        public override string Description =>
            @"
Use this tool to delete a note.
The note with the provided name will be PERMANENTLY DELETED.
";

        protected override void Awake()
        {
            base.Awake();
            notes = GetComponent<LlmNotes>();
        }

        protected override string Execute(DeleteNoteArguments arguments)
        {
            string result;
            try
            {
                notes.Remove(arguments.Name);
                result = "Deleted";
            }
            catch (Exception e)
            {
                result = $"Failed to delete a note: {e.Message}";
            }

            return result;
        }
    }
}
