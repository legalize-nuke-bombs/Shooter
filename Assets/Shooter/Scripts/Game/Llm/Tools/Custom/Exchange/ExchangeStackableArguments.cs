namespace Shooter.Game
{
    public class StackableAmount
    {
        public string Item { get; set; }
        public int Amount { get; set; }
    }

    public class ExchangeStackableArguments
    {
        public long TargetId { get; set; }
        public StackableAmount[] Give { get; set; }
        public StackableAmount[] Take { get; set; }
    }
}
