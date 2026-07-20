using System;
using System.Collections.Generic;
using Shooter.Server.Worlds.Entities;
using Shooter.Server.Worlds.Time;
using Shooter.Server.Worlds.Entities.Parts;
using Shooter.Server.Worlds.Entities.Spawning;
using UnityEngine.SceneManagement;

namespace Shooter.Server.Worlds
{
    public class Players
    {
        private readonly Dictionary<long, Entity> players = new Dictionary<long, Entity>();
        private readonly Scene scene;
        private readonly Clock clock;

        public Players(Scene scene, Clock clock)
        {
            this.scene = scene;
            this.clock = clock;
        }

        public Guid Add(long userId, string displayName)
        {
            Entity player = PlayerSpawner.Spawn(userId, displayName, scene, clock, this);
            players[userId] = player;
            return player.Id;
        }

        public void Remove(long userId)
        {
            if (players.TryGetValue(userId, out Entity player))
            {
                player.Destroy();
                players.Remove(userId);
            }
        }

        public void ApplyInput(long userId, PlayerIntent intent)
        {
            if (players.TryGetValue(userId, out Entity player))
                player.Get<Pilot>()?.Apply(intent);
        }

        public void Tick(float dt)
        {
            foreach (Entity player in players.Values)
                player.Tick(dt);
        }

        public bool AllAsleep()
        {
            if (players.Count == 0) return false;
            foreach (Entity player in players.Values)
            {
                Pilot pilot = player.Get<Pilot>();
                if (pilot == null || !pilot.Sleeping)
                    return false;
            }
            return true;
        }

        public void WakeAll()
        {
            foreach (Entity player in players.Values)
                player.Get<Pilot>()?.WakeUp();
        }

        public void DestroyAll()
        {
            foreach (Entity player in players.Values)
                player.Destroy();
            players.Clear();
        }

        public void CollectStates(Dictionary<Guid, EntityState> into)
        {
            foreach (Entity player in players.Values)
                into[player.Id] = player.State();
        }

        public int Count()
        {
            return players.Count;
        }
    }
}
