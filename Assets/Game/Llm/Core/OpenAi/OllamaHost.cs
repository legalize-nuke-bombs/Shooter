using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine.Networking;

namespace Shooter.Game.Llm
{
    public sealed class OllamaHost : IOpenAiHost
    {
        private const string Host = "localhost:11434";
        private const int TimeoutSeconds = 300;

        private static readonly JsonSerializerSettings Settings = new()
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public async Task<string> Request(string key, OpenAiRequest body, CancellationToken until)
        {
            var uri = new Uri($"http://{Host}/v1/chat/completions");

            using (var webRequest = new UnityWebRequest(uri, "POST"))
            {
                string sent = JsonConvert.SerializeObject(body, Settings);
                webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(sent));
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.timeout = TimeoutSeconds;
                webRequest.SetRequestHeader("Content-Type", "application/json");
                if (!string.IsNullOrEmpty(key)) webRequest.SetRequestHeader("Authorization", $"Bearer {key}");

                var completion = new TaskCompletionSource<bool>();
                webRequest.SendWebRequest().completed += _ => completion.TrySetResult(true);

                using (until.Register(webRequest.Abort))
                {
                    await completion.Task;
                }

                until.ThrowIfCancellationRequested();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                    webRequest.result == UnityWebRequest.Result.ProtocolError)
                    throw new LlmException(
                        $"HTTP {webRequest.responseCode} {webRequest.error}: {webRequest.downloadHandler?.text}");

                string answered = webRequest.downloadHandler?.text;
                if (string.IsNullOrEmpty(answered)) throw new LlmException("Host returned an empty response body");

                return answered;
            }
        }
    }
}
