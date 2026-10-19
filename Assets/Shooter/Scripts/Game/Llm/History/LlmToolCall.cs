using Shooter.Infrastructure;

namespace Shooter.Game
{
    public class LlmToolCall : ISaveable
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Arguments { get; set; }

        private struct SaveData
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Arguments { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData()
            {
                Id = Id,
                Name = Name,
                Arguments = Arguments
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData saveData = content.ToObject<SaveData>();
            Id = saveData.Id;
            Name = saveData.Name;
            Arguments = saveData.Arguments;
        }
    }
}
