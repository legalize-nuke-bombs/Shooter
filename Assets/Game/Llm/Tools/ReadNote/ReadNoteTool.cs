using System;
using Shooter.Game.Llm.Notes;
using UnityEngine;

namespace Shooter.Game.Llm.ReadNote
{
    [RequireComponent(typeof(LlmNotes))]
    public class ReadNoteTool : LlmTool<ReadNoteArguments>
    {
        private LlmNotes notes;

        public override string Name => "read_note";

        public override string Description =>
            @"
Use this tool to read the full content of one note by its name.
";

        protected override void Awake()
        {
            base.Awake();
            notes = GetComponent<LlmNotes>();
        }

        protected override string Execute(ReadNoteArguments arguments, LlmCallContext context)
        {
            string result;
            try
            {
                result = notes.Read(arguments.Name);
            }
            catch (Exception e)
            {
                result = $"Failed to read: {e.Message}";
            }

            return result;
        }
    }
}
