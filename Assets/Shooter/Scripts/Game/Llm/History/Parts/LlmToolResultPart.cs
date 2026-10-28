namespace Shooter.Game
{
    public sealed class LlmToolResultPart : LlmPart
    {
        public LlmToolResultPart(string toolCallId, string text)
        {
            ToolCallId = toolCallId;
            Text = text;
        }

        public string ToolCallId { get; }
        public string Text { get; set; }

        public override int Length => Text.Length;
    }
}
