using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using UnityEngine.Networking;
using Shooter.Configuring;
using Shooter.Logging;

namespace Shooter.Game.Llm.Gemini
{
    public sealed class GeminiLlm : Llm
    {
        private const string Host = "generativelanguage.googleapis.com";
        private const int TimeoutSeconds = 25;
        private const int ExcerptLength = 300;

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = { new StringEnumConverter() }
        };

        protected override async Task<LlmAnswer> Request(string systemPrompt, IReadOnlyList<LlmMessage> messages)
        {
            LlmConfig llm = Config.Read<ServerConfig>(ServerConfig.FileName).Llm;

            if (string.IsNullOrEmpty(llm.Key))
            {
                throw new InvalidOperationException($"Llm key is not set in {ServerConfig.FileName}");
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

            var uri = new Uri($"https://{Host}/v1beta/models/{llm.Model}:generateContent");
            Log.Info("Entity {} is asking {} for an answer", name, llm.Model);

            using (var webRequest = new UnityWebRequest(uri, "POST"))
            {
                webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request, Settings)));
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.timeout = TimeoutSeconds;
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("x-goog-api-key", llm.Key);

                await Completion(webRequest.SendWebRequest());

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"HTTP {webRequest.responseCode} {webRequest.error}: {Excerpt(webRequest.downloadHandler?.text)}");
                }

                var response = JsonConvert.DeserializeObject<GeminiResponse>(webRequest.downloadHandler.text, Settings);
                string text = response?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
                if (string.IsNullOrEmpty(text))
                {
                    throw new Exception("Response has no candidate text: " + Excerpt(webRequest.downloadHandler.text));
                }

                var answer = JsonConvert.DeserializeObject<LlmAnswer>(text, Settings);
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
