using System;
using System.Text;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [Serializable]
    public class DeleteNotesTool : LlmTool<DeleteNotesArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private LlmNotes llmNotes;

        public override string Name => "delete_notes";

        public override string Description =>
            @"
Use this tool to delete notes, one or many in a single call.
The notes with the provided names will be PERMANENTLY DELETED.
";

        protected override void HandleAttached()
        {
            llmNotes = Self.GetComponent<LlmNotes>();
            if (llmNotes == null)
            {
                Log.Error($"Entity {Self.name} does not have llm notes component required by tool {Name}");
            }
        }

        protected override string Execute(DeleteNotesArguments arguments, LlmCallContext context)
        {
            if (arguments.Names == null || arguments.Names.Length == 0) return "Nothing to delete";

            StringBuilder result = new();
            foreach (string noteName in arguments.Names)
            {
                try
                {
                    llmNotes.RemoveNote(noteName);
                    result.Append(noteName).Append(": Deleted").Append('\n');
                }
                catch (Exception e)
                {
                    result.Append(noteName).Append(": Failed - ").Append(e.Message).Append('\n');
                }
            }

            return result.ToString();
        }
    }
}
