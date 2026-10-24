using System;
using System.Text;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [Serializable]
    public class AddNotesTool : LlmTool<AddNotesArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private LlmNotes llmNotes;

        public override string Name => "add_notes";

        public override string Description =>
            @$"
Use this tool to add new notes, one or many in a single call.
The name is a short id: you will retype it exactly to address the note. The description is shown in the notes list: write what is inside and why to open it.
Keep one subject per note. To change existing notes, use update_notes.
Max note name size: {llmNotes.NameLimit}
Max note description size: {llmNotes.DescriptionLimit}
Max note content size: {llmNotes.ContentLimit}
Max notes number: {llmNotes.AmountLimit}
";

        protected override void Start()
        {
            llmNotes = Self.GetComponent<LlmNotes>();
            if (llmNotes == null)
            {
                Log.Error($"Entity {Self.name} does not have llm notes component required by tool {Name}");
            }
        }

        protected override string Execute(AddNotesArguments arguments, LlmCallContext context)
        {
            if (arguments.Notes == null || arguments.Notes.Length == 0) return "Nothing to add";

            StringBuilder result = new();
            foreach (LlmNoteEntry entry in arguments.Notes)
            {
                try
                {
                    llmNotes.AddNote(entry.Name, new LlmNote { Content = entry.Content, Description = entry.Description });
                    result.Append(entry.Name).Append(": Added").Append('\n');
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
