using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Shooter.Game
{
    public class AnthropicRequest
    {
        public string Model { get; set; }

        [JsonProperty("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonProperty("system")]
        public string SystemPrompt { get; set; }

        public AnthropicMessage[] Messages { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public AnthropicTool[] Tools { get; set; }

        public AnthropicThinking Thinking { get; set; }

        [JsonProperty("output_config", NullValueHandling = NullValueHandling.Ignore)]
        public AnthropicOutputConfig OutputConfig { get; set; }

        [JsonProperty("cache_control")]
        public AnthropicCacheControl CacheControl { get; set; }

        public string Fallbacks { get; set; }
    }

    public class AnthropicMessage
    {
        public string Role { get; set; }
        public JArray Content { get; set; }
    }

    public class AnthropicTool
    {
        public string Name { get; set; }
        public string Description { get; set; }

        [JsonProperty("input_schema")]
        public JObject InputSchema { get; set; }
    }

    public class AnthropicThinking
    {
        public string Type { get; set; }
        public string Display { get; set; }

        [JsonProperty("block_binding")]
        public AnthropicBlockBinding BlockBinding { get; set; }
    }

    public class AnthropicBlockBinding
    {
        [JsonProperty("prefix_mismatch_behavior")]
        public string PrefixMismatchBehavior { get; set; }
    }

    public class AnthropicOutputConfig
    {
        public string Effort { get; set; }
    }

    public class AnthropicCacheControl
    {
        public string Type { get; set; }
    }

    public class AnthropicResponse
    {
        public JArray Content { get; set; }

        [JsonProperty("stop_reason")]
        public string StopReason { get; set; }

        [JsonProperty("stop_details")]
        public AnthropicStopDetails StopDetails { get; set; }

        [JsonProperty("input_transformations")]
        public JArray InputTransformations { get; set; }
    }

    public class AnthropicStopDetails
    {
        public string Category { get; set; }
        public string Explanation { get; set; }
    }
}
