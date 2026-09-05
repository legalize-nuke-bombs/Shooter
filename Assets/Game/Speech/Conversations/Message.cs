using System;
using Shooter.Game.Core.Saves;

namespace Shooter.Game.Speech
{
    public class Message : ISaveable
    {
        public const string TimeFormat = "yyyy.MM.dd HH:mm:ss";

        public long AuthorId { get; set; }
        public string Content { get; set; }
        public DateTime Time { get; set; }
        public bool Spoken { get; set; }

        private struct SaveData
        {
            public long AuthorId { get; set; }
            public string Content { get; set; }
            public DateTime Time { get; set; }
            public bool Spoken { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData
            {
                AuthorId = AuthorId,
                Content = Content,
                Time = Time,
                Spoken = Spoken
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.To<SaveData>();
            AuthorId = sd.AuthorId;
            Content = sd.Content;
            Time = sd.Time;
            Spoken = sd.Spoken;
        }
    }
}
