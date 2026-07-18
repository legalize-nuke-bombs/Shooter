using Shooter.Serialization;
using Shooter.Server.Entities.Players;
using Shooter.Server.Entities.Chronology;

namespace Shooter.Server.Worlds
{
    public class Snapshot : Serializable
    {
        public long tick;
        public PlayerState[] players;
        public ClockState clock;

        public Snapshot(long tick, ServerWorld serverWorld)
        {
            this.tick = tick;
            this.players = serverWorld.BuildPlayerStates();
            this.clock = serverWorld.BuildClockState();
        }
    }
}
