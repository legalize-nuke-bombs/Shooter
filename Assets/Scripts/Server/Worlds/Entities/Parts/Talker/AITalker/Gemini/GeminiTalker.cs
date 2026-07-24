using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Shooter.Logging;
using Shooter.Serialization;
using Shooter.Server.Worlds.Time;

namespace Shooter.Server.Worlds.Entities.Parts.Talker.AITalker.Gemini
{
    public sealed class GeminiTalker : AITalker
    {
        private const string Host = "generativelanguage.googleapis.com";
        private const int ExcerptLength = 300;

        private readonly string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        private readonly string model;

        public GeminiTalker(Entity self, Clock clock, string character, GeminiModel model) : base(self, clock, character)
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
                    new GeminiContent { Parts = new[] { new GeminiPart { Text = conversation } } }
                },
                SystemInstruction = new GeminiContent
                {
                    Parts = new[] { new GeminiPart { Text = systemPrompt } }
                }
            };

            var uri = new Uri($"https://{Host}/v1beta/models/{model}:generateContent");
            Log.Info("Entity {} is asking {} for an answer", Self.Name, model);

            using (var webRequest = new UnityWebRequest(uri, "POST"))
            {
                webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(Json.Serialize(request)));
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
                    throw new Exception($"HTTP {webRequest.responseCode} {webRequest.error}: {Excerpt(webRequest.downloadHandler?.text)}");
                }

                GeminiResponse response = Json.Deserialize<GeminiResponse>(webRequest.downloadHandler.text);
                string text = response?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
                if (string.IsNullOrEmpty(text))
                {
                    throw new Exception("Response has no candidate text: " + Excerpt(webRequest.downloadHandler.text));
                }

                return text.Trim();
            }
        }

        private static string Excerpt(string body)
        {
            if (string.IsNullOrEmpty(body)) return "";

            string flat = body.Replace("\n", " ").Replace("\r", "");
            return flat.Length <= ExcerptLength ? flat : flat.Substring(0, ExcerptLength) + "...";
        }
    }
}
