using System.Collections.Generic;
using Shooter.Server.Worlds.Entities.Chronology;
using Shooter.Server.Worlds.Entities.Players;
using UnityEngine.SceneManagement;

namespace Shooter.Server.Worlds
{
    public class ServerWorldPlayers
    {
        private readonly Dictionary<long, Player> players = new Dictionary<long, Player>();
        private readonly Scene scene;
        private readonly Clock clock;

        public ServerWorldPlayers(Scene scene, Clock clock)
        {
            this.scene = scene;
            this.clock = clock;
        }

        public void Add(long userId, string displayName)
        {
            players[userId] = new Player(userId, displayName, scene, clock, this);
        }

        public void Remove(long userId)
        {
            if (players.TryGetValue(userId, out Player player))
            {
                player.Destroy();
                players.Remove(userId);
            }
        }

        public void ApplyInput(long userId, PlayerIntent intent)
        {
            if (players.TryGetValue(userId, out Player player))
                player.ApplyInput(intent);
        }

        public void Tick(float dt)
        {
            foreach (Player player in players.Values)
                player.Tick(dt);
        }

        public bool AllAsleep()
        {
            if (players.Count == 0) return false;
            foreach (Player player in players.Values)
                if (!player.Sleeping)
                    return false;
            return true;
        }

        public void WakeAll()
        {
            foreach (Player player in players.Values)
                player.WakeUp();
        }

        public Dictionary<long, PlayerState> BuildStates()
        {
            var states = new Dictionary<long, PlayerState>(players.Count);
            foreach (Player player in players.Values)
            {
                PlayerState state = player.State();
                states[state.Id] = state;
            }
            return states;
        }

        public int Count()
        {
            return players.Count;
        }
    }
}
