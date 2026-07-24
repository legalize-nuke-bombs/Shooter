using System.Text;

namespace Shooter.Server.Worlds.Entities.Parts.Llm
{
    public sealed class Prompt
    {
        private readonly StringBuilder text = new StringBuilder();

        public Prompt Text(string content)
        {
            if (string.IsNullOrEmpty(content)) return this;

            if (text.Length > 0) text.Append("\n");
            text.Append(content.TrimEnd('\n')).Append("\n");
            return this;
        }

        public Prompt Section(string title, string content)
        {
            if (string.IsNullOrEmpty(content)) return this;

            return Text("## " + title + "\n\n" + content);
        }

        public override string ToString()
        {
            return text.ToString();
        }
    }
}
