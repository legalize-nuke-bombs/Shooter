using System;
using System.Text;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [Serializable]
    public class UpdateNotesTool : LlmTool<UpdateNotesArguments>
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private LlmNotes llmNotes;

        public override string Name => "update_notes";

        public override string Description =>
            @"
Use this tool to replace existing notes, one or many in a single call: the description and content you pass replace the old ones entirely.
Fails on notes that do not exist. To create new notes, use add_notes. The same size limits apply.
";

        protected override void HandleAttached()
        {
            llmNotes = Self.GetComponent<LlmNotes>();
            if (llmNotes == null)
            {
                Log.Error($"Entity {Self.name} does not have llm notes component required by tool {Name}");
            }
        }

        protected override string Execute(UpdateNotesArguments arguments, LlmCallContext context)
        {
            if (arguments.Notes == null || arguments.Notes.Length == 0) return "Nothing to update";

            StringBuilder result = new();
            foreach (LlmNoteEntry entry in arguments.Notes)
            {
                try
                {
                    llmNotes.ReplaceNote(entry.Name, new LlmNote { Content = entry.Content, Description = entry.Description });
                    result.Append(entry.Name).Append(": Updated").Append('\n');
                }
                catch (Exception e)
                {
                    result.Append(entry.Name).Append(": Failed - ").Append(e.Message).Append('\n');
                }
            }

            return result.ToString();
        }
    }
}
