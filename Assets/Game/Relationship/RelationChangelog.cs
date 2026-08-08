namespace Shooter.Game.Relationship
{
    public class RelationChangelog
    {
        public string Time { get; set; }
        public long Id { get; set; }
        public int From { get; set; }
        public int To { get; set; }
        public string Reason { get; set; }
    }
}
