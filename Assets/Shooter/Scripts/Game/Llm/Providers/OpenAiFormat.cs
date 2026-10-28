using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    public sealed class OpenAiFormat : ILlmFormat
    {
        private const string FormatName = "OpenAi";

        public object BuildRequestBody(LlmConfig config, string systemPrompt, IReadOnlyList<LlmMessage> history, IReadOnlyList<ILlmTool> tools)
        {
            var messages = new List<OpenAiMessage> { new() { Role = "system", Content = systemPrompt } };
            messages.AddRange(history.Select(ConvertToOpenAiMessage));

            return new OpenAiRequest
            {
                Model = config.Model,
                Messages = messages.ToArray(),
                Tools = tools == null || tools.Count == 0 ? null : tools.Select(ConvertToOpenAiTool).ToArray(),
                ReasoningEffort = string.IsNullOrEmpty(config.Effort) ? null : config.Effort
            };
        }

        public LlmTurn ParseTurn(string responseJson)
        {
            JObject answeredMessage = ParseAnsweredMessage(responseJson);
            OpenAiMessage message = answeredMessage.ToObject<OpenAiMessage>(LlmJson.Serializer);

            var toolCalls = new List<LlmToolCall>();

            if (message.ToolCalls != null)
            {
                foreach (OpenAiToolCall openAiToolCall in message.ToolCalls)
                {
                    if (openAiToolCall?.Function == null) continue;

                    toolCalls.Add(new LlmToolCall
                    {
                        Id = openAiToolCall.Id,
                        Name = openAiToolCall.Function.Name,
                        Arguments = openAiToolCall.Function.Arguments
                    });
                }
            }

            return new LlmTurn
            {
                Content = message.Content,
                ToolCalls = toolCalls.ToArray(),
                RawMessage = answeredMessage.ToString(Formatting.None),
                RawFormat = FormatName
            };
        }

        private static JObject ParseAnsweredMessage(string responseJson)
        {
            JToken response;
            try
            {
                response = LlmJson.Parse(responseJson);
            }
            catch (JsonException e)
            {
                throw new LlmException($"Failed to parse the provider response {responseJson}: {e.Message}");
            }

            JObject answeredMessage = response?["choices"]?.FirstOrDefault()?["message"] as JObject;
            if (answeredMessage == null) throw new LlmException($"The provider response has no message: {responseJson}");

            return answeredMessage;
        }

        private static OpenAiTool ConvertToOpenAiTool(ILlmTool tool)
        {
            return new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = tool.Name,
                    Description = tool.Description,
                    Parameters = tool.Parameters
                }
            };
        }

        private static OpenAiMessage ConvertToOpenAiMessage(LlmMessage message)
        {
            if (message.RawFormat == FormatName) return LlmJson.Deserialize<OpenAiMessage>(message.RawMessage);

            OpenAiMessage openAiMessage = new()
            {
                Role = message.Role.ToString().ToLowerInvariant(),
                Content = message.Content,
                ToolCallId = message.ToolCallId
            };

            if (message.ToolCalls != null && message.ToolCalls.Length > 0)
            {
                openAiMessage.ToolCalls = message.ToolCalls
                    .Select(toolCall => new OpenAiToolCall
                    {
                        Id = toolCall.Id,
                        Type = "function",
                        Function = new OpenAiCalledFunction { Name = toolCall.Name, Arguments = toolCall.Arguments }
                    })
                    .ToArray();
            }

            return openAiMessage;
        }
    }
}
