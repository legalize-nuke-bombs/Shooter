namespace Shooter.Game.Llm.PublishEarSound
{
    public class PublishEarSoundArguments
    {
        public string EarSoundName { get; set; }
        public bool IncludeEveryone { get; set; }
        public bool IncludeEveryWanderer { get; set; }
        public long[] IncludeCustomIds { get; set; }
    }
}
