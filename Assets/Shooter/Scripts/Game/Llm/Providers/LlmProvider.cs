using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public static class LlmProvider
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly string SessionFolderName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        private static int requestNumber;

        public static async Task<LlmTurn> RequestTurn(LlmConfig config, string requestName, string systemPrompt, IReadOnlyList<LlmMessage> history, IReadOnlyList<ILlmTool> tools, CancellationToken cancellationToken)
        {
            string requestId = $"{++requestNumber:d4}-{requestName}";

            ILlmHost host = LlmHosts.CreateHostFor(config);
            object body = host.Format.BuildRequestBody(config, systemPrompt, history, tools);

            string folderPath = Path.Combine(Application.temporaryCachePath, "LlmRequests", SessionFolderName);
            Directory.CreateDirectory(folderPath);

            // The body carries the whole history, and serializing it on the main thread froze the frame; it holds its own copies of the messages and tool schemas nobody edits, so nothing changes under the serializer
            string requestJson = await Task.Run(() => LlmJson.Serialize(body), cancellationToken);
            string requestPath = Path.Combine(folderPath, $"{requestId}-request.json");
            Log.Info($"Request {requestId}. Input: {requestJson.Length}ch. Will be saved as {requestPath}");
            await File.WriteAllTextAsync(requestPath, requestJson, cancellationToken);

            string responsePath = Path.Combine(folderPath, $"{requestId}-response.json");
            string responseJson = await SendToHost(host, config.Key, config.Model, requestJson, cancellationToken);
            await File.WriteAllTextAsync(responsePath, responseJson, cancellationToken);
            Log.Info($"Response {requestId}. Output: {responseJson.Length}ch. Will be saved as {responsePath}");

            return host.Format.ParseTurn(responseJson);
        }

        private static async Task<string> SendToHost(ILlmHost host, string apiKey, string model, string requestJson, CancellationToken cancellationToken)
        {
            Log.Info($"Sending request. Model: {model}");

            try
            {
                string responseJson = await host.Send(apiKey, requestJson, cancellationToken);
                Log.Info("Response received successfully");

                return responseJson;
            }
            catch (OperationCanceledException)
            {
                Log.Info("Request dropped, the asker is gone");
                throw;
            }
            catch (Exception e)
            {
                Log.Warn($"Request failed. Error: {e.Message}");
                throw;
            }
        }
    }
}
