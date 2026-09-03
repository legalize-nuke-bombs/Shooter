using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Shooter.Configuring;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm
{
    public static class LlmProvider
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly JsonSerializerSettings Settings = new()
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private static readonly string SessionFolder = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        private static int requestNumber;

        public static async Task<LlmTurn> Request(LlmConfig config, string requestName, string system,
            IReadOnlyList<LlmMessage> history, IReadOnlyList<ILlmTool> tools, CancellationToken until)
        {
            string requestId = $"{++requestNumber:d4}-{requestName}";

            var messages = new List<OpenAiMessage> { new() { Role = "system", Content = system } };
            messages.AddRange(history.Select(Mapped));

            var body = new OpenAiRequest
            {
                Model = config.Model,
                Messages = messages.ToArray(),
                Tools = tools == null || tools.Count == 0 ? null : tools.Select(Declared).ToArray()
            };

            string folderPath = Path.Combine(Application.temporaryCachePath, "LlmRequests", SessionFolder);
            Directory.CreateDirectory(folderPath);

            string sent = JsonConvert.SerializeObject(body, Settings);
            string requestPath = Path.Combine(folderPath, $"{requestId}-request.json");
            Log.Info($"Request {requestId}. Input: {sent.Length}ch. Will be saved as {requestPath}");
            await File.WriteAllTextAsync(requestPath, sent, until);

            string responsePath = Path.Combine(folderPath, $"{requestId}-response.json");
            string raw = await Ask(OpenAiHosts.For(config), config.Key, body, until);
            await File.WriteAllTextAsync(responsePath, raw, until);
            Log.Info($"Response {requestId}. Output: {raw.Length}ch. Will be saved as {responsePath}");

            OpenAiResponse response = Deserialize(raw);
            OpenAiMessage answered = response?.Choices?.FirstOrDefault()?.Message;

            return Turned(answered, raw);
        }

        private static OpenAiTool Declared(ILlmTool tool)
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

        private static OpenAiMessage Mapped(LlmMessage message)
        {
            var mapped = new OpenAiMessage
            {
                Role = message.Role.ToString().ToLowerInvariant(),
                Content = message.Content,
                ToolCallId = message.ToolCallId
            };

            if (message.ToolCalls != null && message.ToolCalls.Length > 0)
                mapped.ToolCalls = message.ToolCalls
                    .Select(call => new OpenAiToolCall
                    {
                        Id = call.Id,
                        Type = "function",
                        Function = new OpenAiCalledFunction { Name = call.Name, Arguments = call.Arguments }
                    })
                    .ToArray();

            return mapped;
        }

        private static LlmTurn Turned(OpenAiMessage answered, string raw)
        {
            if (answered == null) throw new LlmException($"The provider response has no message: {raw}");

            var calls = new List<LlmToolCall>();

            if (answered.ToolCalls != null)
                foreach (OpenAiToolCall call in answered.ToolCalls)
                {
                    if (call?.Function == null) continue;

                    calls.Add(new LlmToolCall
                    {
                        Id = call.Id,
                        Name = call.Function.Name,
                        Arguments = call.Function.Arguments
                    });
                }

            return new LlmTurn { Content = answered.Content, ToolCalls = calls.ToArray() };
        }

        private static async Task<string> Ask(IOpenAiHost host, string key, OpenAiRequest body,
            CancellationToken until)
        {
            Log.Info($"Sending request. Model: {body.Model}");

            try
            {
                string raw = await host.Request(key, body, until);
                Log.Info("Response received successfully");

                return raw;
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

        private static OpenAiResponse Deserialize(string raw)
        {
            try
            {
                return JsonConvert.DeserializeObject<OpenAiResponse>(raw, Settings);
            }
            catch (Exception e)
            {
                throw new LlmException($"Failed to parse the provider response {raw}: {e.Message}");
            }
        }
    }
}
