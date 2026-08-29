using System;
using System.Text;
using Shooter.Game.Llm.Notes;
using UnityEngine;

namespace Shooter.Game.Llm.UpdateNotes
{
    [RequireComponent(typeof(LlmNotes))]
    public class UpdateNotesTool : LlmTool<UpdateNotesArguments>
    {
        private LlmNotes notes;

        public override string Name => "update_notes";

        public override string Description =>
            @"
Use this tool to replace existing notes, one or many in a single call: the description and content you pass replace the old ones entirely.
Fails on notes that do not exist. To create new notes, use add_notes. The same size limits apply.
";

        protected override void Awake()
        {
            base.Awake();
            notes = GetComponent<LlmNotes>();
        }

        protected override string Execute(UpdateNotesArguments arguments, LlmCallContext context)
        {
            if (arguments.Notes == null || arguments.Notes.Length == 0) return "Nothing to update";

            var sb = new StringBuilder();
            foreach (LlmNoteEntry entry in arguments.Notes)
            {
                try
                {
                    notes.Replace(entry.Name, new LlmNote { Content = entry.Content, Description = entry.Description });
                    sb.Append(entry.Name).Append(": Updated").Append('\n');
                }
                catch (Exception e)
                {
                    sb.Append(entry.Name).Append(": Failed - ").Append(e.Message).Append('\n');
                }
            }

            return sb.ToString();
        }
    }
}
