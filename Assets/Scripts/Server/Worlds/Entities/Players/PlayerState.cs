namespace Shooter.Server.Worlds.Entities.Players
{
    public class PlayerState
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Yaw { get; set; }
        public float Pitch { get; set; }
        public bool Sleeping { get; set; }
    }
}
