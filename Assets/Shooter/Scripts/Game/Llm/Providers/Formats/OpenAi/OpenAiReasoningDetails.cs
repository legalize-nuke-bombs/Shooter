using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Shooter.Game
{
    public sealed class OpenAiReasoningDetails : IOpenAiReasoning
    {
        private const string FormatName = "OpenAi";

        private static readonly Dictionary<string, string> TextFieldsByType = new()
        {
            ["reasoning.text"] = "text",
            ["reasoning.summary"] = "summary"
        };

        public IEnumerable<LlmReasoningPart> ReadFrom(OpenAiMessage message)
        {
            if (message.ReasoningDetails == null) return Enumerable.Empty<LlmReasoningPart>();

            return MergeFragments(message.ReasoningDetails).Select(detail => new LlmReasoningPart(FormatName, detail.ToString(Formatting.None)));
        }

        public void WriteTo(OpenAiMessage message, IEnumerable<LlmReasoningPart> reasoningParts)
        {
            List<JToken> reasoningDetails = MergeFragments(reasoningParts
                .Where(reasoning => reasoning.Format == FormatName)
                .Select(reasoning => LlmJson.Parse(reasoning.Json)));

            message.ReasoningDetails = reasoningDetails.Count == 0 ? null : new JArray(reasoningDetails);
        }

        // Polza returns some models' reasoning (MiMo) as unmerged stream deltas even in a non-streamed reply
        private static List<JToken> MergeFragments(IEnumerable<JToken> details)
        {
            var merged = new List<JToken>();
            foreach (JToken detail in details)
            {
                if (merged.LastOrDefault() is JObject block && detail is JObject fragment && IsSameBlock(block, fragment))
                {
                    AppendFragment(block, fragment);
                }
                else
                {
                    merged.Add(detail.DeepClone());
                }
            }

            return merged;
        }

        private static bool IsSameBlock(JObject block, JObject fragment)
        {
            string type = (string)block["type"];

            return type != null
                && TextFieldsByType.ContainsKey(type)
                && type == (string)fragment["type"]
                && JToken.DeepEquals(block["index"], fragment["index"]);
        }

        private static void AppendFragment(JObject block, JObject fragment)
        {
            string textField = TextFieldsByType[(string)block["type"]];
            block[textField] = (string)block[textField] + (string)fragment[textField];

            foreach (JProperty property in fragment.Properties())
            {
                if (property.Name == textField) continue;

                JToken existing = block[property.Name];
                if (existing == null || existing.Type == JTokenType.Null) block[property.Name] = property.Value.DeepClone();
            }
        }
    }
}
