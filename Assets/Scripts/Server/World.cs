using System.Collections.Generic;
using Shooter.Net;
using Shooter.Entities.Player;
using Shooter.Entities.Chronology;

namespace Shooter.Server
{
    public class World
    {
        public string Id { get; }
        public float OffsetX { get; }

        private readonly Clock clock = new Clock();
        private readonly Dictionary<int, ServerPlayer> players = new Dictionary<int, ServerPlayer>();

        public World(string id, float offsetX)
        {
            Id = id;
            OffsetX = offsetX;
        }

        public IReadOnlyCollection<ServerPlayer> Players => players.Values;

        public void Add(ServerPlayer player)
        {
            players[player.ConnId] = player;
        }

        public void Remove(int connId)
        {
            players.Remove(connId);
        }

        public void Step(float dt)
        {
            foreach (ServerPlayer p in players.Values)
                ServerPlayerSim.Step(p, dt);
            clock.Advance(dt);
        }

        public PlayerStateMsg[] BuildStates()
        {
            var states = new List<PlayerStateMsg>(players.Count);
            foreach (ServerPlayer p in players.Values)
                states.Add(ServerPlayerSim.BuildState(p));
            return states.ToArray();
        }

        public SnapshotMsg BuildSnapshot(long tick)
        {
            return new SnapshotMsg
            {
                type = "snapshot",
                tick = tick,
                players = BuildStates(),
                clock = clock.BuildState()
            };
        }
    }
}
