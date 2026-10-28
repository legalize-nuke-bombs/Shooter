namespace Shooter.Game
{
    // The model's reasoning in the provider's own record, kept as it came: only the format that read it can send it back
    public sealed class LlmReasoningPart : LlmPart
    {
        public LlmReasoningPart(string format, string json)
        {
            Format = format;
            Json = json;
        }

        public string Format { get; }
        public string Json { get; }

        public override int Length => Json.Length;
    }
}
