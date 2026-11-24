using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Shooter.Game
{
    public sealed class OpenAiReasoningDetails : IOpenAiReasoning
    {
        private const string FormatName = "OpenAi";

        public IEnumerable<LlmReasoningPart> ReadFrom(OpenAiMessage message)
        {
            if (message.ReasoningDetails == null) return Enumerable.Empty<LlmReasoningPart>();

            return message.ReasoningDetails.Select(detail => new LlmReasoningPart(FormatName, detail.ToString(Formatting.None)));
        }

        public void WriteTo(OpenAiMessage message, IEnumerable<LlmReasoningPart> reasoningParts)
        {
            JToken[] reasoningDetails = reasoningParts
                .Where(reasoning => reasoning.Format == FormatName)
                .Select(reasoning => LlmJson.Parse(reasoning.Json))
                .ToArray();

            message.ReasoningDetails = reasoningDetails.Length == 0 ? null : new JArray(reasoningDetails);
        }
    }
}
