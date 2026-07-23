namespace Shooter.Server.Worlds.Entities.Parts.Talker.AITalker.Gemini
{
    public class GeminiRequest
    {
        public Content[] Contents { get; set; }
        public Content SystemInstruction { get; set; }
    }

    public class Content
    {
        public Part[] Parts { get; set; }
    }

    public class Part
    {
        public string Text { get; set; }
    }

    public class GeminiResponse
    {
        public Candidate[] Candidates { get; set; }
    }

    public class Candidate
    {
        public Content Content { get; set; }
    }
}
