using System;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [Serializable]
    public class ListNotesTool : LlmTool<ListNotesArguments>
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private LlmNotes llmNotes;

        public override string Name => "list_notes";

        public override string Description =>
            @"
Use this tool to list your notes: the name, the description and the last update time of each.
To see the content, use read_notes.
";

        protected override void HandleAttached()
        {
            llmNotes = Self.GetComponent<LlmNotes>();
            if (llmNotes == null)
            {
                Log.Error($"Entity {Self.name} does not have LlmNotes component required by tool {Name}");
            }
        }

        protected override string Execute(ListNotesArguments arguments, LlmCallContext context)
        {
            return llmNotes.ListNotes();
        }
    }
}
