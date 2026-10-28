namespace Shooter.Game
{
    public class LlmTurn
    {
        public string Content { get; set; }
        public LlmToolCall[] ToolCalls { get; set; }
        public string RawMessage { get; set; }
        public string RawFormat { get; set; }

        public bool HasToolCalls => ToolCalls != null && ToolCalls.Length > 0;
    }
}
