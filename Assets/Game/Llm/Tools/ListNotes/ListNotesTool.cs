using Shooter.Game.Llm.Notes;
using UnityEngine;

namespace Shooter.Game.Llm.ListNotes
{
    [RequireComponent(typeof(LlmNotes))]
    public class ListNotesTool : LlmTool<ListNotesArguments>
    {
        private LlmNotes notes;

        public override string Name => "list_notes";

        public override string Description =>
            @"
Use this tool to list your notes: the name, the description and the last update time of each.
To see the content, use read_notes.
";

        protected override void Awake()
        {
            base.Awake();
            notes = GetComponent<LlmNotes>();
        }

        protected override string Execute(ListNotesArguments arguments, LlmCallContext context)
        {
            return notes.List();
        }
    }
}
