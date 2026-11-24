using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Shooter.Game
{
    public sealed class OpenAiReasoningText : IOpenAiReasoning
    {
        private const string FormatName = "OpenAiCompatible";
        private const string ReasoningContentField = "reasoning_content";
        private const string ReasoningField = "reasoning";

        public IEnumerable<LlmReasoningPart> ReadFrom(OpenAiMessage message)
        {
            if (!string.IsNullOrEmpty(message.ReasoningContent)) return new[] { CreatePart(ReasoningContentField, message.ReasoningContent) };
            if (!string.IsNullOrEmpty(message.Reasoning)) return new[] { CreatePart(ReasoningField, message.Reasoning) };

            return Enumerable.Empty<LlmReasoningPart>();
        }

        public void WriteTo(OpenAiMessage message, IEnumerable<LlmReasoningPart> reasoningParts)
        {
            LlmReasoningPart reasoning = reasoningParts.LastOrDefault(part => part.Format == FormatName);
            if (reasoning == null) return;

            JToken record = LlmJson.Parse(reasoning.Json);
            message.ReasoningContent = (string)record[ReasoningContentField];
            message.Reasoning = (string)record[ReasoningField];
        }

        private static LlmReasoningPart CreatePart(string field, string text)
        {
            return new LlmReasoningPart(FormatName, new JObject { [field] = text }.ToString(Formatting.None));
        }
    }
}
