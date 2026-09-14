namespace Shooter.Configuring
{
    public static class Upscalers
    {
        public const string Off = "off";
        public const string Dlaa = "dlaa";
        public const string Quality = "dlss-quality";
        public const string Balanced = "dlss-balanced";
        public const string Performance = "dlss-performance";
        public const string Default = Dlaa;

        public static readonly string[] Keys = { Off, Dlaa, Quality, Balanced, Performance };
    }
}
