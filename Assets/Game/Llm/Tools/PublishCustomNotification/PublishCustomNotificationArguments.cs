namespace Shooter.Game.Llm.PublishCustomNotification
{
    public class PublishCustomNotificationArguments
    {
        public string IconName { get; set; }
        public string EarSoundName { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }

        public bool IncludeEveryone { get; set; }
        public bool IncludeEveryWanderer { get; set; }
        public long[] IncludeCustomIds { get; set; }
    }
}
