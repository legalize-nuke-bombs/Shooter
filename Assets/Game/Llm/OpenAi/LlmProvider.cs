using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

        public static async Task<LlmAnswer> Request(LlmConfig config, Prompt prompt, CancellationToken until)
        {
            if (string.IsNullOrEmpty(config.Key))
            {
                throw new InvalidOperationException($"Llm key is not set in {GameConfig.FileName}");
            }

            var spoken = new List<OpenAiMessage>
            {
                new OpenAiMessage { Role = "system", Content = prompt.ToString() }
            };

            var body = new OpenAiRequest
            {
                Model = config.Model,
                Messages = spoken.ToArray(),
                ResponseFormat = new OpenAiResponseFormat { Type = "json_object" }
            };

            string raw = await Ask(OpenAiHosts.For(config), config.Key, body, until);

            var response = JsonConvert.DeserializeObject<OpenAiResponse>(raw, Settings);
            string content = response?.Choices?.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrEmpty(content))
            {
                throw new LlmAnswerException($"Response carries no message content. Raw: {raw}");
            }

            content = CleanJsonString(content);

            LlmAnswer answer;
            try
            {
                answer = JsonConvert.DeserializeObject<LlmAnswer>(content, Settings);
            }
            catch (JsonException e)
            {
                throw new LlmAnswerException($"Answer is not json: {e.Message}. Content: {content}");
            }

            if (string.IsNullOrEmpty(answer?.Reply))
            {
                throw new LlmAnswerException($"Answer json has no reply property. Content: {content}");
            }

            return answer;
        }

        private static async Task<string> Ask(IOpenAiHost host, string key, OpenAiRequest body,
            CancellationToken until)
        {
            Log.Info("Sending request. Model: {}. Payload: {}",
                body.Model, JsonConvert.SerializeObject(body, Settings));

            try
            {
                string raw = await host.Request(key, body, until);
                Log.Info("Response received successfully. Raw content: {}", raw);

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

        private static OpenAiMessage Said(LlmMessage message)
        {
            string text = string.IsNullOrEmpty(message.Time)
                ? message.Content
                : $"[{message.Time}] {message.Content}";

            return new OpenAiMessage
            {
                Role = message.Role == LlmRole.User ? "user" : "assistant",
                Content = text
            };
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
