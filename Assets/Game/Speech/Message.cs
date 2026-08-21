namespace Shooter.Game.Speech
{
    public class Message
    {
        public const string TimeFormat = "yyyy.MM.dd HH:mm:ss";

        public MessageAuthor Author { get; set; }
        public string Content { get; set; }
        public string Time { get; set; }
    }
}
