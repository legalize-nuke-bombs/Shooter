using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public static class LlmProvider
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private static readonly string SessionFolderName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        private static int requestNumber;

        public static async Task<LlmTurn> RequestTurn(LlmConfig config, string requestName, string systemPrompt, IReadOnlyList<LlmMessage> history, IReadOnlyList<ILlmTool> tools, CancellationToken cancellationToken)
        {
            string requestId = $"{++requestNumber:d4}-{requestName}";

            var messages = new List<OpenAiMessage> { new() { Role = "system", Content = systemPrompt } };
            messages.AddRange(history.Select(ConvertToOpenAiMessage));

            OpenAiRequest body = new()
            {
                Model = config.Model,
                Messages = messages.ToArray(),
                Tools = tools == null || tools.Count == 0 ? null : tools.Select(ConvertToOpenAiTool).ToArray()
            };

            string folderPath = Path.Combine(Application.temporaryCachePath, "LlmRequests", SessionFolderName);
            Directory.CreateDirectory(folderPath);

            string requestJson = JsonConvert.SerializeObject(body, JsonSettings);
            string requestPath = Path.Combine(folderPath, $"{requestId}-request.json");
            Log.Info($"Request {requestId}. Input: {requestJson.Length}ch. Will be saved as {requestPath}");
            await File.WriteAllTextAsync(requestPath, requestJson, cancellationToken);

            string responsePath = Path.Combine(folderPath, $"{requestId}-response.json");
            string responseJson = await SendToHost(OpenAiHosts.CreateHostFor(config), config.Key, body, cancellationToken);
            await File.WriteAllTextAsync(responsePath, responseJson, cancellationToken);
            Log.Info($"Response {requestId}. Output: {responseJson.Length}ch. Will be saved as {responsePath}");

            OpenAiResponse response = DeserializeResponse(responseJson);
            OpenAiMessage answeredMessage = response?.Choices?.FirstOrDefault()?.Message;

            return ConvertToLlmTurn(answeredMessage, responseJson);
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

        private static LlmTurn ConvertToLlmTurn(OpenAiMessage answeredMessage, string responseJson)
        {
            if (answeredMessage == null) throw new LlmException($"The provider response has no message: {responseJson}");

            var toolCalls = new List<LlmToolCall>();

            if (answeredMessage.ToolCalls != null)
            {
                foreach (OpenAiToolCall openAiToolCall in answeredMessage.ToolCalls)
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

            return new LlmTurn { Content = answeredMessage.Content, ToolCalls = toolCalls.ToArray() };
        }

        private static async Task<string> SendToHost(IOpenAiHost host, string apiKey, OpenAiRequest body, CancellationToken cancellationToken)
        {
            Log.Info($"Sending request. Model: {body.Model}");

            try
            {
                string responseJson = await host.RequestChatCompletion(apiKey, body, cancellationToken);
                Log.Info("Response received successfully");

                return responseJson;
            }
            catch (OperationCanceledException)
            {
                Log.Info("Request dropped, the asker is gone");
                throw;
            }
            catch (Exception e)
            {
                Log.Warn($"Request failed. Error: {e.Message}");
                throw;
            }
        }

        private static OpenAiResponse DeserializeResponse(string responseJson)
        {
            try
            {
                return JsonConvert.DeserializeObject<OpenAiResponse>(responseJson, JsonSettings);
            }
            catch (Exception e)
            {
                throw new LlmException($"Failed to parse the provider response {responseJson}: {e.Message}");
            }
        }
    }
}
