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
        private const int MaxTokens = 16000;
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
                OutputConfig = string.IsNullOrEmpty(config.Effort) ? null : new AnthropicOutputConfig { Effort = config.Effort },
                CacheControl = new AnthropicCacheControl { Type = "ephemeral" },
                Fallbacks = "default"
            };
        }

        public LlmTurn ParseTurn(string responseJson)
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

            var texts = new List<string>();
            var toolCalls = new List<LlmToolCall>();

            foreach (JToken block in response.Content)
            {
                switch ((string)block["type"])
                {
                    case "text":
                        texts.Add((string)block["text"]);
                        break;
                    case "tool_use":
                        toolCalls.Add(new LlmToolCall
                        {
                            Id = (string)block["id"],
                            Name = (string)block["name"],
                            Arguments = block["input"]?.ToString(Formatting.None) ?? "{}"
                        });
                        break;
                }
            }

            return new LlmTurn
            {
                Content = texts.Count == 0 ? null : string.Join("\n", texts),
                ToolCalls = toolCalls.ToArray(),
                RawMessage = response.Content.ToString(Formatting.None),
                RawFormat = FormatName
            };
        }

        private static AnthropicMessage[] ConvertToAnthropicMessages(IReadOnlyList<LlmMessage> history)
        {
            var messages = new List<AnthropicMessage>();
            JArray userBlocks = null;

            foreach (LlmMessage message in history)
            {
                if (message.Role == LlmRole.Assistant)
                {
                    JArray assistantBlocks = BuildAssistantBlocks(message);
                    if (assistantBlocks.Count == 0) continue;

                    if (messages.Count == 0) messages.Add(new AnthropicMessage { Role = "user", Content = new JArray(BuildTextBlock(ClearedStoryMarker)) });
                    messages.Add(new AnthropicMessage { Role = "assistant", Content = assistantBlocks });
                    userBlocks = null;
                    continue;
                }

                if (message.Role == LlmRole.User && string.IsNullOrEmpty(message.Content)) continue;

                if (userBlocks == null)
                {
                    userBlocks = new JArray();
                    messages.Add(new AnthropicMessage { Role = "user", Content = userBlocks });
                }

                userBlocks.Add(message.Role == LlmRole.Tool ? BuildToolResultBlock(message) : BuildTextBlock(message.Content));
            }

            return messages.ToArray();
        }

        private static JArray BuildAssistantBlocks(LlmMessage message)
        {
            if (message.RawFormat == FormatName) return (JArray)LlmJson.Parse(message.RawMessage);

            var blocks = new JArray();
            if (!string.IsNullOrEmpty(message.Content)) blocks.Add(BuildTextBlock(message.Content));

            if (message.ToolCalls != null)
            {
                foreach (LlmToolCall toolCall in message.ToolCalls)
                {
                    blocks.Add(new JObject
                    {
                        ["type"] = "tool_use",
                        ["id"] = toolCall.Id,
                        ["name"] = toolCall.Name,
                        ["input"] = ParseToolInput(toolCall.Arguments)
                    });
                }
            }

            return blocks;
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

        private static JObject BuildToolResultBlock(LlmMessage message)
        {
            return new JObject
            {
                ["type"] = "tool_result",
                ["tool_use_id"] = message.ToolCallId,
                ["content"] = message.Content ?? ""
            };
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
