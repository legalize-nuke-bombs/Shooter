namespace Shooter.Game.Llm
{
    public enum LlmRole
    {
        User,
        Assistant,
        Tool
    }

    public class LlmMessage
    {
        public LlmRole Role { get; set; }
        public string Content { get; set; }
        public LlmToolCall[] ToolCalls { get; set; }
        public string ToolCallId { get; set; }
    }
}
