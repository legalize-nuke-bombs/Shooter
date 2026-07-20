using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Server.Worlds.Entities;
using Shooter.Server.Worlds.Entities.Parts;
using Shooter.Server.Worlds.Time;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities.Spawning;
using Shooter.Server.Worlds.Sleeping;

namespace Shooter.Server.Worlds
{
    public class ServerWorld
    {
        public string Id { get; }

        private readonly Scene scene;
        private readonly Clock clock = new Clock();
        private readonly Players players;
        private readonly Sleep sleep;
        private readonly List<Entity> npcs = new List<Entity>();

        public ServerWorld(string id)
        {
            Id = id;
            scene = SceneManager.LoadScene("Map", new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D));
            Log.Info("World {} built: additive physics copy of Map, scene handle {}", id, scene.handle);
            players = new Players(scene, clock);
            sleep = new Sleep(clock, players);
            npcs.Add(Npc.Spawn("npc 0", new Vector3(0f, 1.1f, 16f), scene));
        }

        public void Destroy()
        {
            players.DestroyAll();
            foreach (Entity npc in npcs)
                npc.Destroy();
            npcs.Clear();
            SceneManager.UnloadSceneAsync(scene);
            Log.Info("World {} destroyed, scene unloaded", Id);
        }

        public int Online()
        {
            return players.Count();
        }

        public Guid AddPlayer(long userId, string displayName)
        {
            return players.Add(userId, displayName);
        }

        public void RemovePlayer(long userId)
        {
            players.Remove(userId);
        }

        public void ApplyInput(long userId, PlayerIntent intent)
        {
            players.ApplyInput(userId, intent);
        }

        public void Tick(float dt)
        {
            players.Tick(dt);
            clock.Tick(dt * sleep.ClockScale());
            sleep.Tick();
        }

        public Snapshot BuildSnapshot(long tick)
        {
            var entities = new Dictionary<Guid, EntityState>();
            foreach (Entity npc in npcs)
                entities[npc.Id] = npc.State();
            players.CollectStates(entities);

            return new Snapshot
            {
                Tick = tick,
                Clock = clock.State(),
                Sleep = sleep.State(),
                Entities = entities
            };
        }
    }
}
