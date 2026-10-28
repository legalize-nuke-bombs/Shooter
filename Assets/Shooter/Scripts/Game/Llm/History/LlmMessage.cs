using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    public class LlmMessage : ISaveableObject
    {
        public LlmMessage()
        {
        }

        public LlmMessage(LlmRole role, IEnumerable<LlmPart> parts)
        {
            Role = role;
            Parts = parts.ToList();
        }

        public LlmMessage(LlmRole role, string text) : this(role, new LlmPart[] { new LlmTextPart(text) })
        {
        }

        public LlmRole Role { get; private set; }
        public List<LlmPart> Parts { get; private set; } = new();

        public IEnumerable<LlmToolCallPart> ToolCalls => Parts.OfType<LlmToolCallPart>();
        public string Text => string.Join("\n", Parts.OfType<LlmTextPart>().Select(part => part.Text));

        private enum PartType
        {
            Text,
            ToolCall,
            ToolResult,
            Reasoning
        }

        private struct PartData
        {
            public PartType Type { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Text { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string ToolCallId { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Name { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Arguments { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Format { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Json { get; set; }
        }

        private struct SaveData
        {
            public LlmRole Role { get; set; }
            public List<PartData> Parts { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData
            {
                Role = Role,
                Parts = Parts.Select(SavePart).ToList()
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData saveData = content.ToObject<SaveData>();
            Role = saveData.Role;
            Parts = saveData.Parts.Select(LoadPart).ToList();
        }

        private static PartData SavePart(LlmPart part)
        {
            switch (part)
            {
                case LlmTextPart text:
                    return new PartData { Type = PartType.Text, Text = text.Text };
                case LlmToolCallPart toolCall:
                    return new PartData { Type = PartType.ToolCall, ToolCallId = toolCall.Id, Name = toolCall.Name, Arguments = toolCall.Arguments };
                case LlmToolResultPart toolResult:
                    return new PartData { Type = PartType.ToolResult, ToolCallId = toolResult.ToolCallId, Text = toolResult.Text };
                case LlmReasoningPart reasoning:
                    return new PartData { Type = PartType.Reasoning, Format = reasoning.Format, Json = reasoning.Json };
                default:
                    throw new ArgumentOutOfRangeException(nameof(part), part.GetType().Name, "Unknown llm part");
            }
        }

        private static LlmPart LoadPart(PartData data)
        {
            switch (data.Type)
            {
                case PartType.Text:
                    return new LlmTextPart(data.Text);
                case PartType.ToolCall:
                    return new LlmToolCallPart(data.ToolCallId, data.Name, data.Arguments);
                case PartType.ToolResult:
                    return new LlmToolResultPart(data.ToolCallId, data.Text);
                case PartType.Reasoning:
                    return new LlmReasoningPart(data.Format, data.Json);
                default:
                    throw new ArgumentOutOfRangeException(nameof(data), data.Type, "Unknown llm part type");
            }
        }
    }
}
