using System;
using System.Text;
using Shooter.Game.Llm.Notes;
using UnityEngine;

namespace Shooter.Game.Llm.ReadNotes
{
    [RequireComponent(typeof(LlmNotes))]
    public class ReadNotesTool : LlmTool<ReadNotesArguments>
    {
        private LlmNotes notes;

        public override string Name => "read_notes";

        public override string Description =>
            @"
Use this tool to read the full content of notes by their names, one or many in a single call.
";

        protected override void Awake()
        {
            base.Awake();
            notes = GetComponent<LlmNotes>();
        }

        protected override string Execute(ReadNotesArguments arguments, LlmCallContext context)
        {
            if (arguments.Names == null || arguments.Names.Length == 0) return "Nothing to read";

            var sb = new StringBuilder();
            foreach (string name in arguments.Names)
            {
                sb.Append("== ").Append(name).Append(" ==").Append('\n');
                try
                {
                    sb.Append(notes.Read(name));
                }
                catch (Exception e)
                {
                    sb.Append(e.Message);
                }

                sb.Append('\n').Append('\n');
            }

            return sb.ToString();
        }
    }
}
