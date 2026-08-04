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
            Log.Info("Request {}. Input: {}ch. Will be saved as {}", requestId, promptRaw.Length, promptPath);
            await File.WriteAllTextAsync(promptPath, promptRaw, until);

            string responsePath = Path.Combine(folderPath, $"{requestId}.json");
            string raw = await Ask(OpenAiHosts.For(config), config.Key, body, until);
            await File.WriteAllTextAsync(responsePath, raw, until);
            Log.Info("Response {}. Output: {}ch. Will be saved as {}", requestId, raw.Length, responsePath);

            OpenAiResponse response = Deserialize(raw);
            string content = response?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
            Count(promptRaw.Length, content.Length, response?.Usage);

            return ParseAnswer(content, raw);
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

            Log.Info("Session totals: {} requests, input {} chars / {} tokens, output {} chars / {} tokens",
                sessionRequests, sessionCharsIn, sessionTokensIn, sessionCharsOut, sessionTokensOut);
        }


        private static async Task<string> Ask(IOpenAiHost host, string key, OpenAiRequest body,
            CancellationToken until)
        {
            Log.Info("Sending request. Model: {}", body.Model);

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
                Log.Error("Request failed. Error: {}", e.Message);
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
