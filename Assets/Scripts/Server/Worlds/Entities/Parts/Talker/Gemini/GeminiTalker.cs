using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Shooter.Logging;
using Shooter.Serialization;
using Shooter.Server.Worlds.Time;

namespace Shooter.Server.Worlds.Entities.Parts.Talker.Gemini
{
    public sealed class GeminiTalker : Talker
    {
        private const string Host = "generativelanguage.googleapis.com";

        private readonly string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        private readonly Clock clock;
        private readonly string character;
        private readonly string model;

        public GeminiTalker(Entity self, Clock clock, string character, GeminiModel model) : base(self)
        {
            this.clock = clock;
            this.character = character;
            this.model = model.ToRaw();
        }

        protected override async Task<string> Answer(Conversation conversation)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("GEMINI_API_KEY environment variable is not set");
            }

            string systemPrompt = TalkPrompt.System(Self, conversation, clock, character);
            string dialog = TalkPrompt.Dialog(conversation);

            var request = new GeminiRequest
            {
                Contents = new[]
                {
                    new GeminiContent { Parts = new[] { new GeminiPart { Text = dialog } } }
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
                    throw new Exception($"HTTP {webRequest.responseCode} {webRequest.error}: {webRequest.downloadHandler?.text}");
                }

                GeminiResponse response = Json.Deserialize<GeminiResponse>(webRequest.downloadHandler.text);
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
