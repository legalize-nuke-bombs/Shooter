using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Shooter.Configuring;
using Shooter.Logging;

namespace Shooter.Game.Llm.OpenAi
{
    public static class LlmProvider
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private static int sessionRequests;
        private static long sessionCharsIn;
        private static long sessionCharsOut;
        private static long sessionTokensIn;
        private static long sessionTokensOut;

        public static async Task<LlmAnswer> Request(LlmConfig config, Prompt prompt, CancellationToken until)
        {
            if (string.IsNullOrEmpty(config.Key))
            {
                throw new InvalidOperationException($"Llm key is not set in {GameConfig.FileName}");
            }

            string promptRaw = prompt.ToString();

            string requestId = Guid.NewGuid().ToString();

            var spoken = new List<OpenAiMessage>
            {
                new OpenAiMessage { Role = "system", Content = promptRaw }
            };

            var body = new OpenAiRequest
            {
                Model = config.Model,
                Messages = spoken.ToArray(),
                ResponseFormat = new OpenAiResponseFormat { Type = "json_object" }
            };

            string folderPath = Path.Combine(UnityEngine.Application.temporaryCachePath, "LlmRequests");
            Directory.CreateDirectory(folderPath);

            string promptPath = Path.Combine(folderPath, $"{requestId}.md");
            Log.Info($"Request {requestId}. Input: {promptRaw.Length}ch. Will be saved as {promptPath}");
            await File.WriteAllTextAsync(promptPath, promptRaw, until);

            string responsePath = Path.Combine(folderPath, $"{requestId}.json");
            string raw = await Ask(OpenAiHosts.For(config), config.Key, body, until);
            await File.WriteAllTextAsync(responsePath, raw, until);
            Log.Info($"Response {requestId}. Output: {raw.Length}ch. Will be saved as {responsePath}");

            OpenAiResponse response = Deserialize(raw);
            string content = response?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
            Count(promptRaw.Length, content.Length, response?.Usage);

            return ParseAnswer(content, raw);
        }

        public static async Task<LlmTurn> Request(LlmConfig config, IReadOnlyList<OpenAiMessage> messages,
            IReadOnlyList<OpenAiTool> tools, CancellationToken until)
        {
            if (string.IsNullOrEmpty(config.Key))
            {
                throw new InvalidOperationException($"Llm key is not set in {GameConfig.FileName}");
            }

            string requestId = Guid.NewGuid().ToString();

            var body = new OpenAiRequest
            {
                Model = config.Model,
                Messages = messages.ToArray(),
                Tools = tools == null || tools.Count == 0 ? null : tools.ToArray()
            };

            string folderPath = Path.Combine(UnityEngine.Application.temporaryCachePath, "LlmRequests");
            Directory.CreateDirectory(folderPath);

            string tapeRaw = Rendered(messages);
            string promptPath = Path.Combine(folderPath, $"{requestId}.md");
            Log.Info($"Request {requestId}. Input: {tapeRaw.Length}ch. Will be saved as {promptPath}");
            await File.WriteAllTextAsync(promptPath, tapeRaw, until);

            string responsePath = Path.Combine(folderPath, $"{requestId}.json");
            string raw = await Ask(OpenAiHosts.For(config), config.Key, body, until);
            await File.WriteAllTextAsync(responsePath, raw, until);
            Log.Info($"Response {requestId}. Output: {raw.Length}ch. Will be saved as {responsePath}");

            OpenAiResponse response = Deserialize(raw);
            OpenAiMessage answered = response?.Choices?.FirstOrDefault()?.Message;
            Count(tapeRaw.Length, answered?.Content?.Length ?? 0, response?.Usage);

            return Turned(answered, raw);
        }

        private static string Rendered(IReadOnlyList<OpenAiMessage> messages)
        {
            var text = new StringBuilder();

            foreach (OpenAiMessage message in messages)
            {
                text.Append("## ").Append(message.Role);
                if (!string.IsNullOrEmpty(message.ToolCallId)) text.Append(" (").Append(message.ToolCallId).Append(")");
                text.Append("\n");

                if (!string.IsNullOrEmpty(message.Content)) text.Append(message.Content).Append("\n");

                if (message.ToolCalls != null)
                {
                    foreach (OpenAiToolCall call in message.ToolCalls)
                    {
                        text.Append("-> ").Append(call.Function?.Name).Append(" ").Append(call.Function?.Arguments).Append("\n");
                    }
                }

                text.Append("\n");
            }

            return text.ToString();
        }

        private static LlmTurn Turned(OpenAiMessage answered, string raw)
        {
            if (answered == null)
            {
                throw new LlmAnswerException($"The provider response has no message: {raw}");
            }

            var calls = new List<LlmToolCall>();

            if (answered.ToolCalls != null)
            {
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
            }

            return new LlmTurn { Content = answered.Content, ToolCalls = calls.ToArray() };
        }

        private static void Count(int charsIn, int charsOut, OpenAiUsage usage)
        {
            sessionRequests++;
            sessionCharsIn += charsIn;
            sessionCharsOut += charsOut;

            if (usage != null)
            {
                sessionTokensIn += usage.PromptTokens;
                sessionTokensOut += usage.CompletionTokens;
            }

            Log.Info($"Session totals: {sessionRequests} requests, input {sessionCharsIn} chars / {sessionTokensIn} tokens, output {sessionCharsOut} chars / {sessionTokensOut} tokens");
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
                Log.Error($"Request failed. Error: {e.Message}");
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
                throw new LlmAnswerException($"Failed to parse the provider response {raw}: {e.Message}");
            }
        }

        private static LlmAnswer ParseAnswer(string content, string raw)
        {
            try
            {
                content = CleanJsonString(content);
                return JsonConvert.DeserializeObject<LlmAnswer>(content, Settings);
            }
            catch (Exception e)
            {
                throw new LlmAnswerException($"Failed to parse llm response {raw}: {e.Message}");
            }
        }

        private static string CleanJsonString(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            int firstBracket = text.IndexOf('{');
            int lastBracket = text.LastIndexOf('}');

            if (firstBracket == -1 || lastBracket == -1 || firstBracket >= lastBracket)
                return "";

            return text.Substring(firstBracket, lastBracket - firstBracket + 1);
        }
    }
}
