using Shooter.Game.Core.Saves;

namespace Shooter.Game.Llm
{
    public class LlmMessage : ISaveable
    {
        public LlmRole Role { get; set; }
        public string Content { get; set; }
        public LlmToolCall[] ToolCalls { get; set; }
        public string ToolCallId { get; set; }

        private struct SaveData
        {
            public LlmRole Role { get; set; }
            public string Content { get; set; }
            public LlmToolCall[] ToolCalls { get; set; }
            public string ToolCallId { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData()
            {
                Role = Role,
                Content = Content,
                ToolCalls = ToolCalls,
                ToolCallId = ToolCallId
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.To<SaveData>();
            Role = sd.Role;
            Content = sd.Content;
            ToolCalls = sd.ToolCalls;
            ToolCallId = sd.ToolCallId;
        }
    }
}
