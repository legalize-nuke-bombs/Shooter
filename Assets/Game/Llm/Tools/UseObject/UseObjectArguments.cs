namespace Shooter.Game.Llm.UseObject
{
    public class UseObjectArguments
    {
        public Point3D[] Points { get; set; }
    }

    public class Point3D
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }
}
