namespace Shooter.Server.Worlds.Items
{
    public abstract class UniqueItem
    {
        public long Id { get; }

        protected UniqueItem(long id)
        {
            Id = id;
        }

        public abstract UniqueItemState State();
    }

    public abstract class UniqueItemState
    {
        public long Id { get; set; }
    }
}
