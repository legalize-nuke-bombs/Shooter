using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Shooter.Configuring;

namespace Shooter.Game.Llm
{
    public static class LlmProvider
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public static async Task<LlmAnswer> Request(LlmConfig config, Prompt basePrompt, IReadOnlyList<LlmMessage> messages)
        {
            if (string.IsNullOrEmpty(config.Key))
            {
                throw new InvalidOperationException($"Llm key is not set in {GameConfig.FileName}");
            }

            var networkMessages = new List<OpenAiMessage>();
            networkMessages.Add(new OpenAiMessage { Role = "system", Content = basePrompt.ToString() });
            networkMessages.AddRange(messages.Select(BuildContent));

            var requestBody = new OpenAiRequest
            {
                Model = config.Model,
                Messages = networkMessages.ToArray(),
                ResponseFormat = new OpenAiResponseFormat { Type = "json_object" }
            };

            ILlmApiProvider apiProvider = LlmApiProviders.For(config);
            string rawResponse = await apiProvider.Request(config.Key, requestBody);

            var apiResponse = JsonConvert.DeserializeObject<OpenAiResponse>(rawResponse, Settings);
            string textContent = apiResponse?.Choices?.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrEmpty(textContent))
            {
                throw new Exception($"API response contains no message content. Raw: {rawResponse}");
            }

            textContent = CleanMarkdown(textContent);

            var answer = JsonConvert.DeserializeObject<LlmAnswer>(textContent, Settings);
            if (string.IsNullOrEmpty(answer?.Reply))
            {
                throw new Exception($"Parsed answer JSON has no reply property. Content: {textContent}");
            }

            return answer;
        }

        private static OpenAiMessage BuildContent(LlmMessage message)
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

        private static string CleanMarkdown(string text)
        {
            return string.IsNullOrEmpty(text)
                ? ""
                : Regex.Replace(text.Trim(), @"^```(?:json)?\s*|\s*```$", "", RegexOptions.IgnoreCase);
        }
    }
}
