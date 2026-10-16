namespace Shooter.Infrastructure
{
    // Why the player is back in the menu, or never left it, in words for the player
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
