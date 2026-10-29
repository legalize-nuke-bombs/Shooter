namespace Shooter.Infrastructure
{
    public static class FogQualities
    {
        public const string Low = "low";
        public const string Medium = "medium";
        public const string High = "high";
        public const string Max = "max";
        public const string Default = Low;

        public static readonly string[] Keys = { Low, Medium, High, Max };
    }
}
