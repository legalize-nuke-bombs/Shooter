using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    public sealed class AnthropicFormat : ILlmFormat
    {
        private const string FormatName = "Anthropic";
        private const int MaxTokens = 64000;
        // The API takes only a user message first, while after clear_head the story starts with the call itself
        private const string ClearedStoryMarker = "[Everything before this point is gone from your head]";

        private static readonly Journal Log = Logs.Here();

        public object BuildRequestBody(LlmConfig config, string systemPrompt, IReadOnlyList<LlmMessage> history, IReadOnlyList<ILlmTool> tools)
        {
            return new AnthropicRequest
            {
                Model = config.Model,
                MaxTokens = MaxTokens,
                SystemPrompt = systemPrompt,
                Messages = ConvertToAnthropicMessages(history),
                Tools = tools == null || tools.Count == 0 ? null : tools.Select(ConvertToAnthropicTool).ToArray(),
                Thinking = new AnthropicThinking
                {
                    Type = "adaptive",
                    Display = "summarized",
                    BlockBinding = new AnthropicBlockBinding { PrefixMismatchBehavior = "drop_block" }
                },
                OutputConfig = new AnthropicOutputConfig { Effort = config.Effort },
                CacheControl = new AnthropicCacheControl { Type = "ephemeral" },
                Fallbacks = "default"
            };
        }

        public LlmMessage ParseReply(string responseJson)
        {
            AnthropicResponse response;
            try
            {
                response = LlmJson.Deserialize<AnthropicResponse>(responseJson);
            }
            catch (JsonException e)
            {
                throw new LlmException($"Failed to parse the provider response {responseJson}: {e.Message}");
            }

            if (response?.Content == null) throw new LlmException($"The provider response has no content: {responseJson}");
            if (response.StopReason == "refusal") throw new LlmException($"The model refused, category {response.StopDetails?.Category}: {response.StopDetails?.Explanation}");
            if (response.StopReason == "max_tokens") throw new LlmException($"The reply hit the limit of {MaxTokens} tokens");

            if (response.InputTransformations != null && response.InputTransformations.Count > 0)
            {
                Log.Info($"The provider dropped thinking blocks: {response.InputTransformations.ToString(Formatting.None)}");
            }

            var parts = new List<LlmPart>();
            foreach (JToken block in response.Content)
            {
                string type = (string)block["type"];
                switch (type)
                {
                    case "text":
                        parts.Add(new LlmTextPart((string)block["text"]));
                        break;
                    case "tool_use":
                        parts.Add(new LlmToolCallPart((string)block["id"], (string)block["name"], block["input"]?.ToString(Formatting.None) ?? "{}"));
                        break;
                    case "thinking":
                    case "redacted_thinking":
                        parts.Add(new LlmReasoningPart(FormatName, block.ToString(Formatting.None)));
                        break;
                    default:
                        Log.Warn($"The provider sent an unknown content block {type}, it is dropped");
                        break;
                }
            }

            return new LlmMessage(LlmRole.Assistant, parts);
        }

        private static AnthropicMessage[] ConvertToAnthropicMessages(IReadOnlyList<LlmMessage> history)
        {
            var messages = new List<AnthropicMessage>();
            JArray userBlocks = null;

            foreach (LlmMessage message in history)
            {
                if (message.Role == LlmRole.Assistant)
                {
                    var assistantBlocks = new JArray(message.Parts.Select(BuildAssistantBlock).Where(block => block != null));
                    if (assistantBlocks.Count == 0) continue;

                    if (messages.Count == 0) messages.Add(new AnthropicMessage { Role = "user", Content = new JArray(BuildTextBlock(ClearedStoryMarker)) });
                    messages.Add(new AnthropicMessage { Role = "assistant", Content = assistantBlocks });
                    userBlocks = null;
                    continue;
                }

                foreach (JObject block in message.Parts.Select(BuildUserBlock).Where(block => block != null))
                {
                    if (userBlocks == null)
                    {
                        userBlocks = new JArray();
                        messages.Add(new AnthropicMessage { Role = "user", Content = userBlocks });
                    }

                    userBlocks.Add(block);
                }
            }

            return messages.ToArray();
        }

        private static JToken BuildAssistantBlock(LlmPart part)
        {
            switch (part)
            {
                case LlmTextPart text:
                    return string.IsNullOrEmpty(text.Text) ? null : BuildTextBlock(text.Text);
                case LlmToolCallPart toolCall:
                    return new JObject
                    {
                        ["type"] = "tool_use",
                        ["id"] = toolCall.Id,
                        ["name"] = toolCall.Name,
                        ["input"] = ParseToolInput(toolCall.Arguments)
                    };
                case LlmReasoningPart reasoning:
                    return reasoning.Format == FormatName ? LlmJson.Parse(reasoning.Json) : null;
                default:
                    return null;
            }
        }

        private static JObject BuildUserBlock(LlmPart part)
        {
            switch (part)
            {
                case LlmTextPart text:
                    return string.IsNullOrEmpty(text.Text) ? null : BuildTextBlock(text.Text);
                case LlmToolResultPart toolResult:
                    return new JObject
                    {
                        ["type"] = "tool_result",
                        ["tool_use_id"] = toolResult.ToolCallId,
                        ["content"] = toolResult.Text
                    };
                default:
                    return null;
            }
        }

        private static JObject ParseToolInput(string arguments)
        {
            if (string.IsNullOrEmpty(arguments)) return new JObject();

            try
            {
                return LlmJson.Parse(arguments) as JObject ?? new JObject();
            }
            catch (JsonException)
            {
                return new JObject();
            }
        }

        private static JObject BuildTextBlock(string text)
        {
            return new JObject { ["type"] = "text", ["text"] = text };
        }

        private static AnthropicTool ConvertToAnthropicTool(ILlmTool tool)
        {
            return new AnthropicTool
            {
                Name = tool.Name,
                Description = tool.Description,
                InputSchema = tool.Parameters
            };
        }
    }
}
