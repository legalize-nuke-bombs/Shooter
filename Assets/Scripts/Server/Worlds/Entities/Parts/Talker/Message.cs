namespace Shooter.Server.Worlds.Entities.Parts.Talker
{
    public class Message
    {
        public MessageAuthor Author { get; set; }
        public string Content { get; set; }
        public MessageMetadata Metadata { get; set; }

        public MessageState State()
        {
            return new MessageState
            {
                Author = Author,
                Content = Content
            };
        }
    }
}
