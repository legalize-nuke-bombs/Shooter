namespace Shooter.Server.Worlds.Entities.Parts.Talker.AITalker.Gemini
{
    public class GeminiRequest
    {
        public GeminiContent[] Contents { get; set; }
        public GeminiContent SystemInstruction { get; set; }
    }

    public class GeminiContent
    {
        public GeminiPart[] Parts { get; set; }
    }

    public class GeminiPart
    {
        public string Text { get; set; }
    }

    public class GeminiResponse
    {
        public GeminiCandidate[] Candidates { get; set; }
    }

    public class GeminiCandidate
    {
        public GeminiContent Content { get; set; }
    }
}
