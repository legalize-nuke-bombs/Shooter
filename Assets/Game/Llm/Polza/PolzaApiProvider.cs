using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine.Networking;

namespace Shooter.Game.Llm.Polza
{
    public sealed class PolzaApiProvider : ILlmApiProvider
    {
        private const string Host = "api.polza.ai";
        private const int TimeoutSeconds = 25;

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public async Task<string> RequestRaw(string apiKey, OpenAiRequest requestBody)
        {
            var uri = new Uri($"https://{Host}/v1/chat/completions");

            using (var webRequest = new UnityWebRequest(uri, "POST"))
            {
                string jsonToSend = JsonConvert.SerializeObject(requestBody, Settings);
                webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonToSend));
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.timeout = TimeoutSeconds;
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                var completion = new TaskCompletionSource<bool>();
                webRequest.SendWebRequest().completed += _ => completion.SetResult(true);
                await completion.Task;

                if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                    webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    throw new Exception($"HTTP {webRequest.responseCode} {webRequest.error}: {webRequest.downloadHandler?.text}");
                }

                string responseText = webRequest.downloadHandler?.text;
                if (string.IsNullOrEmpty(responseText))
                {
                    throw new Exception("API returned an empty response body.");
                }

                return responseText;
            }
        }
    }
}
