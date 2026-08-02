namespace Shooter.Game.Llm
{
    public class OpenAiRequest
    {
        public string Model { get; set; }
        public OpenAiMessage[] Messages { get; set; }
        public OpenAiResponseFormat ResponseFormat { get; set; }
    }

    public class OpenAiMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    public class OpenAiResponseFormat
    {
        public string Type { get; set; }
    }

    public class OpenAiResponse
    {
        public OpenAiChoice[] Choices { get; set; }
    }

    public class OpenAiChoice
    {
        public OpenAiMessage Message { get; set; }
    }
}
