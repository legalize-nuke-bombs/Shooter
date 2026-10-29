namespace Shooter.Infrastructure
{
    public static class LlmEfforts
    {
        public const string Low = "low";
        public const string Medium = "medium";
        public const string High = "high";
        public const string Max = "max";
        public const string Default = High;

        public static readonly string[] Keys = { Low, Medium, High, Max };
    }
}
