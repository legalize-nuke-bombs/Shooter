using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Shooter.Logging;

namespace Shooter.Game.Llm
{
    public interface ILlmApiProvider
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly JsonSerializerSettings LogSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        async Task<string> Request(string apiKey, OpenAiRequest requestBody)
        {
            Log.Info("[LLM API] Sending request. Model: {}. Payload: {}", requestBody?.Model, JsonConvert.SerializeObject(requestBody, LogSettings));
            try
            {
                string rawResponse = await RequestRaw(apiKey, requestBody);
                Log.Info("[LLM API] Response received successfully. Raw content: {}", rawResponse);
                return rawResponse;
            }
            catch (Exception ex)
            {
                Log.Error("[LLM API] Request failed. Error: {}", ex.Message);
                throw;
            }
        }

        Task<string> RequestRaw(string apiKey, OpenAiRequest requestBody);
    }
}
