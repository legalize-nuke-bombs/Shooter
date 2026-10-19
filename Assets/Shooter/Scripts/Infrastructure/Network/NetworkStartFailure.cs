namespace Shooter.Infrastructure
{
    public sealed class NetworkStartFailure
    {
        public NetworkStartFailure(string title, string details)
        {
            Title = title;
            Details = details;
        }

        public string Title { get; }

        public string Details { get; }
    }
}
