using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Shooter.Logging;

namespace Shooter.Server.Worlds.Entities.Parts.Talker.Gemini
{
    public class GeminiTalker : Talker
    {
        private readonly string apiKey;
        private readonly string model;
        private readonly GeminiSettings settings = new GeminiSettings();
        private readonly string characterSystemPrompt;

        private readonly JsonSerializerSettings jsonSettings;
        private readonly ConcurrentDictionary<long, SemaphoreSlim> userLocks = new ConcurrentDictionary<long, SemaphoreSlim>();
        public GeminiTalker(GeminiModel model, string characterSystemPrompt, Health.Health health) : base(health)
        {
            this.model = model.ToRaw();
            this.characterSystemPrompt = characterSystemPrompt;
            apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
        }

        public override void StartTalking(long userId)
        {
            _ = StartTalkingAsync(userId);
        }

        private async Task StartTalkingAsync(long userId)
        {
            var semaphore = userLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));

            if (!await semaphore.WaitAsync(0))
            {
                return;
            }

            string aiResponseText = "";

            try
            {
                string systemPrompt = settings.BaseSystemPrompt + "\n" + characterSystemPrompt;
                string userPrompt = Conversations[userId].ToString();

                Log.Info("Requesting Gemini response for user {} system prompt {} user prompt {}...", userId, systemPrompt, userPrompt); // TODO remove messages from logs

                aiResponseText = await SendGeminiRequestAsync(
                    systemPrompt,
                    userPrompt
                );

                if (string.IsNullOrEmpty(aiResponseText))
                {
                    throw new Exception("AI Response Text is null");
                }
            }
            catch (Exception e)
            {
                aiResponseText = "I can't talk right now.";
                Log.Error("Critical error in StartTalking for user {}: {}", userId, e.Message);
            }
            finally
            {
                semaphore.Release();

                var message = new Message
                {
                    Author = MessageAuthor.Talker,
                    Content = aiResponseText
                };

                Conversations[userId].Add(message);
                Log.Info("Talking to {}: {}", userId, message.Content); // TODO remove message Content
            }
        }

        private async Task<string> SendGeminiRequestAsync(string systemPrompt, string userPrompt)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                Log.Error("API key is missing in environment variables.");
                return null;
            }

            const string path = "v1beta/models";
            const string action = "generateContent";
            const string host = "generativelanguage.googleapis.com";

            var uriBuilder = new UriBuilder
            {
                Scheme = "https",
                Host = host,
                Path = $"{path}/{model}:{action}",
                Query = $"key={apiKey}"
            };

            Uri uri = uriBuilder.Uri;

            var requestData = new GeminiRequest
            {
                Contents = new[]
                {
                    new Content
                    {
                        Parts = new[] { new Part { Text = userPrompt } }
                    }
                },
                SystemInstruction = new Content
                {
                    Parts = new[] { new Part { Text = systemPrompt } }
                }
            };

            string jsonBody = JsonConvert.SerializeObject(requestData, jsonSettings);
            byte[] rawBody = Encoding.UTF8.GetBytes(jsonBody);

            using (var webRequest = new UnityWebRequest(uri, "POST"))
            {
                webRequest.uploadHandler = new UploadHandlerRaw(rawBody);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                var operation = webRequest.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Log.Error("HTTP Error {} for user request: {}. Response: {}", webRequest.responseCode, webRequest.error, webRequest.downloadHandler?.text);
                    return null;
                }

                string responseJson = webRequest.downloadHandler.text;
                var responseData = JsonConvert.DeserializeObject<GeminiResponse>(responseJson);

                if (responseData?.Candidates != null &&
                    responseData.Candidates.Length > 0 &&
                    responseData.Candidates[0]?.Content?.Parts != null &&
                    responseData.Candidates[0].Content.Parts.Length > 0 &&
                    responseData.Candidates[0].Content.Parts[0]?.Text != null)
                {
                    return responseData.Candidates[0].Content.Parts[0].Text.Trim();
                }

                Log.Error("Incomplete JSON response layout from Gemini API. Response: {}", responseJson);
                return null;
            }
        }
    }
}
