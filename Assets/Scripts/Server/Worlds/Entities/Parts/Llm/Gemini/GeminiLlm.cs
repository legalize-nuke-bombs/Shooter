using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Shooter.Logging;
using Shooter.Serialization;
using Shooter.Server.Worlds.Time;

namespace Shooter.Server.Worlds.Entities.Parts.Llm.Gemini
{
    public sealed class GeminiLlm : Llm
    {
        private const string Host = "generativelanguage.googleapis.com";
        private const int TimeoutSeconds = 25;
        private const int ExcerptLength = 300;

        private readonly string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        private readonly string model = Environment.GetEnvironmentVariable("GEMINI_MODEL");

        public GeminiLlm(Entity self, Clock clock, string character) : base(self, clock, character)
        {
        }

        protected override async Task<LlmAnswer> Request(string systemPrompt, IReadOnlyList<LlmMessage> messages)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("GEMINI_API_KEY environment variable is not set");
            }

            var request = new GeminiRequest
            {
                Contents = messages.Select(Content).ToArray(),
                SystemInstruction = new GeminiContent
                {
                    Parts = new[] { new GeminiPart { Text = systemPrompt } }
                },
                GenerationConfig = new GeminiGenerationConfig
                {
                    ResponseMimeType = "application/json",
                    ResponseSchema = AnswerSchema()
                }
            };

            var uri = new Uri($"https://{Host}/v1beta/models/{model}:generateContent");
            Log.Info("Entity {} is asking {} for an answer", Self.Name, model);

            using (var webRequest = new UnityWebRequest(uri, "POST"))
            {
                webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(Json.Serialize(request)));
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.timeout = TimeoutSeconds;
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("x-goog-api-key", apiKey);

                await Completion(webRequest.SendWebRequest());

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"HTTP {webRequest.responseCode} {webRequest.error}: {Excerpt(webRequest.downloadHandler?.text)}");
                }

                GeminiResponse response = Json.Deserialize<GeminiResponse>(webRequest.downloadHandler.text);
                string text = response?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
                if (string.IsNullOrEmpty(text))
                {
                    throw new Exception("Response has no candidate text: " + Excerpt(webRequest.downloadHandler.text));
                }

                LlmAnswer answer = Json.Deserialize<LlmAnswer>(text);
                if (string.IsNullOrEmpty(answer?.Reply))
                {
                    throw new Exception("Answer json has no reply: " + Excerpt(text));
                }

                return answer;
            }
        }

        private static Task Completion(UnityWebRequestAsyncOperation operation)
        {
            var completion = new TaskCompletionSource<bool>();
            operation.completed += _ => completion.SetResult(true);
            return completion.Task;
        }

        private static GeminiSchema AnswerSchema()
        {
            return new GeminiSchema
            {
                Type = "OBJECT",
                Properties = new Dictionary<string, GeminiSchema>
                {
                    ["reply"] = new GeminiSchema { Type = "STRING" },
                    ["memory"] = new GeminiSchema { Type = "STRING", Nullable = true }
                },
                Required = new[] { "reply" }
            };
        }

        private static GeminiContent Content(LlmMessage message)
        {
            string text = string.IsNullOrEmpty(message.Time)
                ? message.Content
                : $"[{message.Time}] {message.Content}";

            return new GeminiContent
            {
                Role = message.Role == LlmRole.User ? "user" : "model",
                Parts = new[] { new GeminiPart { Text = text } }
            };
        }

        private static string Excerpt(string body)
        {
            if (string.IsNullOrEmpty(body)) return "";

            string flat = body.Replace("\n", " ").Replace("\r", "");
            return flat.Length <= ExcerptLength ? flat : flat.Substring(0, ExcerptLength) + "...";
        }
    }
}
