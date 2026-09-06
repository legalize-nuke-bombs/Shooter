namespace Shooter.Game.Llm.GoTo
{
    public class GoToArguments
    {
        public string TaskName { get; set; }
        public int Bearing { get; set; }
        public int Distance { get; set; }
        public bool Sprint { get; set; }
        public bool Force { get; set; }
    }
}
