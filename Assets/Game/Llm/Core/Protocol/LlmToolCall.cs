using Newtonsoft.Json.Linq;
using Shooter.Game.Core.Saves;

namespace Shooter.Game.Llm
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
        public void LoadObject(JToken content)
        {
            SaveData sd = content.ToObject<SaveData>();
            Id = sd.Id;
            Name = sd.Name;
            Arguments = sd.Arguments;
        }
    }
}
