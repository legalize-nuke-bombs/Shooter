using Newtonsoft.Json.Linq;
using Shooter.Game.Core.Saves;

namespace Shooter.Game.Llm.Notes
{
    public class LlmNote : ISaveable
    {
        public string Description { get; set; }
        public string Content { get; set; }

        private struct SaveData
        {
            public string Description { get; set; }
            public string Content { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData()
            {
                Description = Description,
                Content = Content
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.To<SaveData>();
            Description = sd.Description;
            Content = sd.Content;
        }
    }
}
