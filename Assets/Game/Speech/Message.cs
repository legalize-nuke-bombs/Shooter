using Shooter.Game.Core.Saves;

namespace Shooter.Game.Speech
{
    public class Message : ISaveable
    {
        public const string TimeFormat = "yyyy.MM.dd HH:mm:ss";

        public MessageAuthor Author { get; set; }
        public string Content { get; set; }
        public string Time { get; set; }

        private struct SaveData
        {
            public MessageAuthor Author { get; set; }
            public string Content { get; set; }
            public string Time { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData()
            {
                Author = Author,
                Content = Content,
                Time = Time
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.To<SaveData>();
            Author = sd.Author;
            Content = sd.Content;
            Time = sd.Time;
        }
    }
}
