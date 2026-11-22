namespace Shooter.Infrastructure
{
    public static class ReflectionModes
    {
        public const string Off = "off";
        public const string ScreenSpace = "screen-space";
        public const string Planar = "planar";
        public const string Default = Off;

        public static readonly string[] Keys = { Off, ScreenSpace, Planar };
    }
}
