using System;
using System.Text;
using Shooter.Game.Llm.Notes;
using Shooter.Logging;

namespace Shooter.Game.Llm.AddNotes
{
    [Serializable]
    public class AddNotesTool : LlmTool<AddNotesArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private LlmNotes notes;

        public override string Name => "add_notes";

        public override string Description =>
            @$"
Use this tool to add new notes, one or many in a single call.
The name is a short id: you will retype it exactly to address the note. The description is shown in the notes list: write what is inside and why to open it.
Keep one subject per note. To change existing notes, use update_notes.
Max note name size: {notes.NameLimit}
Max note description size: {notes.DescriptionLimit}
Max note content size: {notes.ContentLimit}
Max notes number: {notes.AmountLimit}
";

        public override void OnStart(LlmInitContext context)
        {
            notes = context.Self.GetComponent<LlmNotes>();
            if (notes == null)
            {
                Log.Error($"Entity {context.Self.name} does not have llm notes component required by tool {Name}");
            }
        }

        protected override string Execute(AddNotesArguments arguments, LlmCallContext context)
        {
            if (arguments.Notes == null || arguments.Notes.Length == 0) return "Nothing to add";

            var sb = new StringBuilder();
            foreach (LlmNoteEntry entry in arguments.Notes)
            {
                try
                {
                    notes.Add(entry.Name, new LlmNote { Content = entry.Content, Description = entry.Description });
                    sb.Append(entry.Name).Append(": Added").Append('\n');
                }
                catch (Exception e)
                {
                    sb.Append(entry.Name).Append(": Failed - ").Append(e.Message).Append('\n');
                }
            }

            return sb.ToString();
        }
    }
}
