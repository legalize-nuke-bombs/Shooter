namespace Shooter.Server.Worlds.Entities.Parts.Pilot
{
    public class PlayerIntent
    {
        public float MoveX { get; set; }
        public float MoveZ { get; set; }
        public bool Jump { get; set; }
        public bool Sprint { get; set; }
        public bool Use { get; set; }
        public bool Shoot { get; set; }
        public float Yaw { get; set; }
        public float Pitch { get; set; }
    }
}
