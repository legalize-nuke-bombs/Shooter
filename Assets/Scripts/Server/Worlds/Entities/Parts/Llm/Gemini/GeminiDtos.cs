using System.Collections.Generic;

namespace Shooter.Server.Worlds.Entities.Parts.Llm.Gemini
{
    public class GeminiRequest
    {
        public GeminiContent[] Contents { get; set; }
        public GeminiContent SystemInstruction { get; set; }
        public GeminiGenerationConfig GenerationConfig { get; set; }
    }

    public class GeminiGenerationConfig
    {
        public string ResponseMimeType { get; set; }
        public GeminiSchema ResponseSchema { get; set; }
    }

    public class GeminiSchema
    {
        public string Type { get; set; }
        public Dictionary<string, GeminiSchema> Properties { get; set; }
        public string[] Required { get; set; }
        public bool? Nullable { get; set; }
    }

    public class GeminiContent
    {
        public string Role { get; set; }
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
