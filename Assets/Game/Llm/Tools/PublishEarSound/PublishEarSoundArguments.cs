namespace Shooter.Game.Llm.PublishEarSound
{
    public class PublishEarSoundArguments
    {
        public string EarSoundName { get; set; }
        public bool IncludeEveryWanderer { get; set; }
        public long[] IncludeCustomWanderers { get; set; }
    }
}
