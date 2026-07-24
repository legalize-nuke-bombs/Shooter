namespace Shooter.Server.Worlds.Entities.Parts.Llm
{
    public class LlmMessage
    {
        public LlmRole Role { get; set; }
        public string Content { get; set; }
        public string Time { get; set; }
    }
}
