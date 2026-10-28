using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shooter.Infrastructure;
using UnityEngine.Networking;

namespace Shooter.Game
{
    public sealed class ClaudeHost : IOpenAiHost
    {
        private const string Host = "api.anthropic.com";
        private const int TimeoutSeconds = 25;

        public async Task<string> RequestChatCompletion(string apiKey, string requestJson, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException($"Claude requires a key. Set it in {GameConfig.FileName}");
            }

            Uri uri = new($"https://{Host}/v1/messages");

            using (UnityWebRequest webRequest = new(uri, "POST"))
            {
                webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestJson));
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.timeout = TimeoutSeconds;

                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("x-api-key", apiKey);
                webRequest.SetRequestHeader("anthropic-version", "2023-06-01");

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
