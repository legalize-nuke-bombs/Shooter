using Shooter.Infrastructure;

namespace Shooter.Game
{
    public class LlmNote : ISaveableObject
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
            SaveData saveData = content.ToObject<SaveData>();
            Description = saveData.Description;
            Content = saveData.Content;
            Updated = saveData.Updated;
        }
    }
}
