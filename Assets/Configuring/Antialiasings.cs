namespace Shooter.Configuring
{
    public static class Antialiasings
    {
        public const string Off = "off";
        public const string Fxaa = "fxaa";
        public const string Taa = "taa";
        public const string Smaa = "smaa";
        public const string Default = Smaa;

        public static readonly string[] Keys = { Off, Fxaa, Taa, Smaa };
    }
}
