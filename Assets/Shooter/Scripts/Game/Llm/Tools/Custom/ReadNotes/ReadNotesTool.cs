using System;
using System.Text;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [Serializable]
    public class ReadNotesTool : LlmTool<ReadNotesArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private LlmNotes llmNotes;

        public override string Name => "read_notes";

        public override string Description =>
            @"
Use this tool to read the full content of notes by their names, one or many in a single call.
";

        protected override void Start()
        {
            llmNotes = Self.GetComponent<LlmNotes>();
            if (llmNotes == null)
            {
                Log.Error($"Entity {Self.name} does not have LlmNotes component required by tool {Name}");
            }
        }

        protected override string Execute(ReadNotesArguments arguments, LlmCallContext context)
        {
            if (arguments.Names == null || arguments.Names.Length == 0) return "Nothing to read";

            StringBuilder result = new();
            foreach (string noteName in arguments.Names)
            {
                result.Append("== ").Append(noteName).Append(" ==").Append('\n');
                try
                {
                    result.Append(llmNotes.ReadNoteContent(noteName));
                }
                catch (Exception e)
                {
                    result.Append(e.Message);
                }

                result.Append('\n').Append('\n');
            }

            return result.ToString();
        }
    }
}
