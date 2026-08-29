using System;
using Shooter.Game.Llm.Notes;
using UnityEngine;

namespace Shooter.Game.Llm.UpdateNote
{
    [RequireComponent(typeof(LlmNotes))]
    public class UpdateNoteTool : LlmTool<UpdateNoteArguments>
    {
        private LlmNotes notes;

        public override string Name => "update_note";

        public override string Description =>
            @"
Use this tool to replace an existing note: the description and content you pass replace the old ones entirely.
Fails if the note does not exist. To create a new note, use add_note. The same size limits apply.
";

        protected override void Awake()
        {
            base.Awake();
            notes = GetComponent<LlmNotes>();
        }

        protected override string Execute(UpdateNoteArguments arguments, LlmCallContext context)
        {
            string result;
            try
            {
                notes.Replace(
                    arguments.Name,
                    new LlmNote
                    {
                        Content = arguments.Content,
                        Description = arguments.Description
                    }
                );
                result = "Updated";
            }
            catch (Exception e)
            {
                result = $"Failed to update a note: {e.Message}";
            }

            return result;
        }
    }
}
