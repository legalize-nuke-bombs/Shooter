using System.Text;

namespace Shooter.Game.Llm
{
    public sealed class Prompt
    {
        private readonly StringBuilder text = new StringBuilder();

        public Prompt Section(string title, string content)
        {
            if (string.IsNullOrEmpty(content)) return this;

            return Text("## " + title + "\n\n" + content);
        }

        public Prompt Section(Prompt section)
        {
            return Text(section.ToString());
        }

        private Prompt Text(string content)
        {
            if (string.IsNullOrEmpty(content)) return this;

            if (text.Length > 0) text.Append("\n");
            text.Append(content.TrimEnd('\n')).Append("\n");
            return this;
        }

        public override string ToString()
        {
            return text.ToString();
        }
    }
}
