using System;
using System.Text;
using Shooter.Game.Llm.Notes;
using UnityEngine;

namespace Shooter.Game.Llm.DeleteNotes
{
    [RequireComponent(typeof(LlmNotes))]
    public class DeleteNotesTool : LlmTool<DeleteNotesArguments>
    {
        private LlmNotes notes;

        public override string Name => "delete_notes";

        public override string Description =>
            @"
Use this tool to delete notes, one or many in a single call.
The notes with the provided names will be PERMANENTLY DELETED.
";

        protected override void Awake()
        {
            base.Awake();
            notes = GetComponent<LlmNotes>();
        }

        protected override string Execute(DeleteNotesArguments arguments, LlmCallContext context)
        {
            if (arguments.Names == null || arguments.Names.Length == 0) return "Nothing to delete";

            var sb = new StringBuilder();
            foreach (string name in arguments.Names)
            {
                try
                {
                    notes.Remove(name);
                    sb.Append(name).Append(": Deleted").Append('\n');
                }
                catch (Exception e)
                {
                    sb.Append(name).Append(": Failed - ").Append(e.Message).Append('\n');
                }
            }

            return sb.ToString();
        }
    }
}
