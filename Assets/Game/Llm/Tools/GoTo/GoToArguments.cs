namespace Shooter.Game.Llm.GoTo
{
    public class GoToArguments
    {
        public string TaskName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public bool Sprint { get; set; }
        public bool Force { get; set; }
    }
}
