namespace Shooter.Server.Worlds.Entities.Parts.Pilot
{
    public class PilotState : PartState
    {
        public long UserId { get; set; }
        public float Pitch { get; set; }
        public bool Sleeping { get; set; }
    }
}
