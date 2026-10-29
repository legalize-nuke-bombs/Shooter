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
            foreach (LlmMessage message in history)
            {
                messages.AddRange(ConvertToOpenAiMessages(message));
            }

            return new OpenAiRequest
            {
                Model = config.Model,
                Messages = messages.ToArray(),
                Tools = tools == null || tools.Count == 0 ? null : tools.Select(ConvertToOpenAiTool).ToArray(),
                ReasoningEffort = ConvertToReasoningEffort(config.Effort)
            };
        }

        public LlmMessage ParseReply(string responseJson)
        {
            OpenAiMessage message = ParseAnsweredMessage(responseJson);

            var parts = new List<LlmPart>();

            if (message.ReasoningDetails != null)
            {
                parts.AddRange(message.ReasoningDetails.Select(detail => new LlmReasoningPart(FormatName, detail.ToString(Formatting.None))));
            }

            if (!string.IsNullOrEmpty(message.Content)) parts.Add(new LlmTextPart(message.Content));

            if (message.ToolCalls != null)
            {
                parts.AddRange(message.ToolCalls
                    .Where(toolCall => toolCall?.Function != null)
                    .Select(toolCall => new LlmToolCallPart(toolCall.Id, toolCall.Function.Name, toolCall.Function.Arguments)));
            }

            return new LlmMessage(LlmRole.Assistant, parts);
        }

        private static OpenAiMessage ParseAnsweredMessage(string responseJson)
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

            return answeredMessage.ToObject<OpenAiMessage>(LlmJson.Serializer);
        }

        private static IEnumerable<OpenAiMessage> ConvertToOpenAiMessages(LlmMessage message)
        {
            switch (message.Role)
            {
                case LlmRole.Assistant:
                    return new[] { ConvertAssistantMessage(message) };
                case LlmRole.Tool:
                    return message.Parts
                        .OfType<LlmToolResultPart>()
                        .Select(toolResult => new OpenAiMessage { Role = "tool", ToolCallId = toolResult.ToolCallId, Content = toolResult.Text });
                default:
                    return new[] { new OpenAiMessage { Role = "user", Content = message.Text } };
            }
        }

        private static OpenAiMessage ConvertAssistantMessage(LlmMessage message)
        {
            OpenAiToolCall[] toolCalls = message.ToolCalls
                .Select(toolCall => new OpenAiToolCall
                {
                    Id = toolCall.Id,
                    Type = "function",
                    Function = new OpenAiCalledFunction { Name = toolCall.Name, Arguments = toolCall.Arguments }
                })
                .ToArray();

            JToken[] reasoningDetails = message.Parts
                .OfType<LlmReasoningPart>()
                .Where(reasoning => reasoning.Format == FormatName)
                .Select(reasoning => LlmJson.Parse(reasoning.Json))
                .ToArray();

            string text = message.Text;

            return new OpenAiMessage
            {
                Role = "assistant",
                Content = string.IsNullOrEmpty(text) ? null : text,
                ToolCalls = toolCalls.Length == 0 ? null : toolCalls,
                ReasoningDetails = reasoningDetails.Length == 0 ? null : new JArray(reasoningDetails)
            };
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

        // OpenAI's reasoning effort tops out at high, there is no max
        private static string ConvertToReasoningEffort(string effort)
        {
            return effort == LlmEfforts.Max ? LlmEfforts.High : effort;
        }
    }
}
