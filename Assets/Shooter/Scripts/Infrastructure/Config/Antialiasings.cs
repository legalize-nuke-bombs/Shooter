namespace Shooter.Infrastructure
{
    public static class Antialiasings
    {
        public const string Off = "off";
        public const string Fxaa = "fxaa";
        public const string Smaa = "smaa";
        public const string Taa = "taa";
        public const string Default = Off;

        public static readonly string[] Keys = { Off, Fxaa, Smaa, Taa };
    }
}
