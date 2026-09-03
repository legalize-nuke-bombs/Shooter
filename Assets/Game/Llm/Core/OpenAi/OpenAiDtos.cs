using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Shooter.Game.Llm
{
    public class OpenAiRequest
    {
        public string Model { get; set; }
        public OpenAiMessage[] Messages { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public OpenAiTool[] Tools { get; set; }

        [JsonProperty("reasoning_effort", NullValueHandling = NullValueHandling.Ignore)]
        public string ReasoningEffort { get; set; }
    }

    public class OpenAiMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }

        [JsonProperty("tool_calls", NullValueHandling = NullValueHandling.Ignore)]
        public OpenAiToolCall[] ToolCalls { get; set; }

        [JsonProperty("tool_call_id", NullValueHandling = NullValueHandling.Ignore)]
        public string ToolCallId { get; set; }
    }

    public class OpenAiTool
    {
        public string Type { get; set; } = "function";
        public OpenAiFunction Function { get; set; }
    }

    public class OpenAiFunction
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public JObject Parameters { get; set; }
    }

    public class OpenAiToolCall
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public OpenAiCalledFunction Function { get; set; }
    }

    public class OpenAiCalledFunction
    {
        public string Name { get; set; }
        public string Arguments { get; set; }
    }

    public class OpenAiResponse
    {
        public OpenAiChoice[] Choices { get; set; }
        public OpenAiUsage Usage { get; set; }
    }

    public class OpenAiChoice
    {
        public OpenAiMessage Message { get; set; }
    }

    public class OpenAiUsage
    {
        [JsonProperty("prompt_tokens")] public int PromptTokens { get; set; }

        [JsonProperty("completion_tokens")] public int CompletionTokens { get; set; }
    }
}
