using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Shooter.Server.Worlds.Time;

namespace Shooter.Server.Worlds.Entities.Parts.Talker.AITalker.Gemini
{
    public class GeminiTalker : AITalker
    {
        private const string Host = "generativelanguage.googleapis.com";

        private readonly string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        private readonly string model;

        private readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public GeminiTalker(GeminiModel model, string characterSystemPrompt, Health.Health health, Clock clock) : base(characterSystemPrompt, health, clock)
        {
            this.model = model.ToRaw();
        }

        protected override async Task<string> RequestAnswer(string systemPrompt, string conversation)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("GEMINI_API_KEY environment variable is not set");
            }

            var request = new GeminiRequest
            {
                Contents = new[]
                {
                    new Content { Parts = new[] { new Part { Text = conversation } } }
                },
                SystemInstruction = new Content
                {
                    Parts = new[] { new Part { Text = systemPrompt } }
                }
            };

            var uri = new Uri($"https://{Host}/v1beta/models/{model}:generateContent");

            using (var webRequest = new UnityWebRequest(uri, "POST"))
            {
                webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request, jsonSettings)));
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("x-goog-api-key", apiKey);

                UnityWebRequestAsyncOperation operation = webRequest.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"HTTP {webRequest.responseCode} {webRequest.error}: {webRequest.downloadHandler?.text}");
                }

                var response = JsonConvert.DeserializeObject<GeminiResponse>(webRequest.downloadHandler.text);
                string text = response?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
                if (string.IsNullOrEmpty(text))
                {
                    throw new Exception("Response has no candidate text: " + webRequest.downloadHandler.text);
                }

                return text.Trim();
            }
        }
    }
}
