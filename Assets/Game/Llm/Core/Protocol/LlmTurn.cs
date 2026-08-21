namespace Shooter.Game.Llm
{
    public class LlmTurn
    {
        public string Content { get; set; }
        public LlmToolCall[] ToolCalls { get; set; }

        public bool CallsTools => ToolCalls != null && ToolCalls.Length > 0;
    }
}
