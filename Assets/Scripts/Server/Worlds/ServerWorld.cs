using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Server.Worlds.Entities;
using Shooter.Server.Worlds.Entities.Parts.Pilot;
using Shooter.Server.Worlds.Entities.Spawning;
using Shooter.Server.Worlds.Time;
using Shooter.Server.Worlds.Sleeping;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities.Parts.Nameable;

namespace Shooter.Server.Worlds
{
    public class ServerWorld
    {
        public string Id { get; }

        private readonly Scene scene;
        private readonly Clock clock = new Clock();
        private readonly WorldEntities entities = new WorldEntities();
        private readonly Sleep sleep;
        private readonly Sight sight;

        public ServerWorld(string id)
        {
            Id = id;
            scene = SceneManager.LoadScene("Map", new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D));
            Log.Info("World {} built: additive physics copy of Map, scene handle {}", id, scene.handle);
            sight = new Sight(scene.GetPhysicsScene());
            sleep = new Sleep(clock, entities);
            entities.Add(NpcSpawner.Spawn(NameableType.Capsule, "", new Vector3(0f, 1.1f, 16f), scene));
            entities.Add(NpcSpawner.Spawn(NameableType.SpecialCorrupted, "", new Vector3(5f, 1.1f, 16f), scene));
        }

        public void Destroy()
        {
            entities.DestroyAll();
            SceneManager.UnloadSceneAsync(scene);
            Log.Info("World {} destroyed, scene unloaded", Id);
        }

        public int Online()
        {
            return entities.PlayerCount;
        }

        public Guid AddPlayer(long userId, string displayName)
        {
            Entity player = PlayerSpawner.Spawn(userId, displayName, scene, sight, clock, entities);
            entities.AddPlayer(userId, player);
            return player.Id;
        }

        public void RemovePlayer(long userId)
        {
            entities.RemovePlayer(userId);
        }

        public void ApplyInput(long userId, PlayerIntent intent)
        {
            entities.ApplyInput(userId, intent);
        }

        public void Tick(float dt)
        {
            entities.Tick(dt);
            clock.Tick(dt * sleep.ClockScale());
            sleep.Tick();
        }

        public Snapshot BuildSnapshot(long tick)
        {
            var states = new Dictionary<Guid, EntityState>();
            entities.CollectStates(states);

            return new Snapshot
            {
                Tick = tick,
                Clock = clock.State(),
                Sleep = sleep.State(),
                Entities = states
            };
        }
    }
}
