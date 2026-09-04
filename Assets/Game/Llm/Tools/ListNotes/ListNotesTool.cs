using System;
using Shooter.Game.Llm.Notes;
using Shooter.Logging;

namespace Shooter.Game.Llm.ListNotes
{
    [Serializable]
    public class ListNotesTool : LlmTool<ListNotesArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private LlmNotes notes;

        public override string Name => "list_notes";

        public override string Description =>
            @"
Use this tool to list your notes: the name, the description and the last update time of each.
To see the content, use read_notes.
";

        public override void OnStart(LlmInitContext context)
        {
            notes = context.Self.GetComponent<LlmNotes>();
            if (notes == null)
            {
                Log.Error($"Entity {context.Self.name} does not have LlmNotes component required by tool {Name}");
            }
        }

        protected override string Execute(ListNotesArguments arguments, LlmCallContext context)
        {
            return notes.List();
        }
    }
}
