namespace Shooter.Infrastructure
{
    public static class Upscalers
    {
        public const string Off = "off";
        public const string Dlss = "dlss";
        public const string Stp = "stp";
        public const string Default = Stp;

        public static readonly string[] Keys = { Off, Dlss, Stp };
    }
}
