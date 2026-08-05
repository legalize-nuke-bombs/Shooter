namespace Shooter.Game.Llm
{
    public class LlmMessage
    {
        public LlmRole Role { get; set; }
        public string Content { get; set; }
        public string Time { get; set; }

        public int PayloadSize => (Role.ToString().Length + Content.Length + Time.Length + 20);
    }
}
