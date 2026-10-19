namespace Shooter.Infrastructure
{
    public sealed class Refusal
    {
        public Refusal(string title, string details)
        {
            Title = title;
            Details = details;
        }

        public string Title { get; }

        public string Details { get; }
    }
}
