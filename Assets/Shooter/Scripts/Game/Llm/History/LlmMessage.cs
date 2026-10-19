using Shooter.Infrastructure;

namespace Shooter.Game
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
            SaveData saveData = content.ToObject<SaveData>();
            Role = saveData.Role;
            Content = saveData.Content;
            ToolCalls = saveData.ToolCalls;
            ToolCallId = saveData.ToolCallId;
        }
    }
}
