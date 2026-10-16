using Newtonsoft.Json.Linq;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    public class LlmNote : ISaveable
    {
        public string Description { get; set; }
        public string Content { get; set; }
        public string Updated { get; set; }

        private struct SaveData
        {
            public string Description { get; set; }
            public string Content { get; set; }
            public string Updated { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData()
            {
                Description = Description,
                Content = Content,
                Updated = Updated
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.To<SaveData>();
            Description = sd.Description;
            Content = sd.Content;
            Updated = sd.Updated;
        }
    }
}
