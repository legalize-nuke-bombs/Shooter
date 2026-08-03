namespace Shooter.Game.Llm
{
    public sealed class Inbox
    {
        private string content = "";

        public void Put(string entry)
        {
            content += entry + "\n";
        }

        public string Take()
        {
            string taken = content;
            content = "";
            return taken;
        }

        public void Return(string taken)
        {
            content = taken + content;
        }
    }
}
