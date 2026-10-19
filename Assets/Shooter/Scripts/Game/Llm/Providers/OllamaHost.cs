using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine.Networking;

namespace Shooter.Game
{
    public sealed class OllamaHost : IOpenAiHost
    {
        private const string Host = "localhost:11434";
        private const int TimeoutSeconds = 300;

        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public async Task<string> RequestChatCompletion(string apiKey, OpenAiRequest body, CancellationToken cancellationToken)
        {
            Uri uri = new($"http://{Host}/v1/chat/completions");

            using (UnityWebRequest webRequest = new(uri, "POST"))
            {
                string requestJson = JsonConvert.SerializeObject(body, JsonSettings);
                webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestJson));
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.timeout = TimeoutSeconds;
                webRequest.SetRequestHeader("Content-Type", "application/json");
                if (!string.IsNullOrEmpty(apiKey)) webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                TaskCompletionSource<bool> completion = new();
                webRequest.SendWebRequest().completed += _ => completion.TrySetResult(true);

                using (cancellationToken.Register(webRequest.Abort))
                {
                    await completion.Task;
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    throw new LlmException($"HTTP {webRequest.responseCode} {webRequest.error}: {webRequest.downloadHandler?.text}");
                }

                string responseJson = webRequest.downloadHandler?.text;
                if (string.IsNullOrEmpty(responseJson)) throw new LlmException("Host returned an empty response body");

                return responseJson;
            }
        }
    }
}
