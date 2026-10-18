namespace Shooter.Infrastructure
{
    public static class Upscalers
    {
        public const string Off = "off";
        public const string Dlaa = "dlaa";
        public const string Quality = "dlss-quality";
        public const string Balanced = "dlss-balanced";
        public const string Performance = "dlss-performance";
        public const string Default = Quality;

        public static readonly string[] CheapestToBest = { Off, Performance, Balanced, Quality, Dlaa };
    }
}
