namespace Shooter.Infrastructure
{
    public static class RayTracingModes
    {
        public const string Off = "off";
        public const string Mixed = "mixed";
        public const string Full = "full";
        public const string Default = Off;

        public static readonly string[] Keys = { Off, Mixed, Full };
    }
}
