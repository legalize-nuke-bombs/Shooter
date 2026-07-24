using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Shooter.Logging;
using Shooter.Server.Protocol;
using Shooter.Server.Worlds.Entities;

namespace Shooter.Server.Worlds
{
    public class WorldEntities
    {
        private readonly Scene scene;
        private readonly Dictionary<Guid, Entity> all = new Dictionary<Guid, Entity>();
        private readonly Dictionary<long, Entity> byUser = new Dictionary<long, Entity>();
        private readonly List<Entity> spawning = new List<Entity>();
        private readonly List<Entity> despawning = new List<Entity>();

        public int PlayerCount => byUser.Count;

        public WorldEntities(Scene scene)
        {
            this.scene = scene;
        }

        public void Add(Entity entity)
        {
            entity.MoveToScene(scene);
            spawning.Add(entity);
            Log.Info("Entity {} spawned at {}", entity.Name, entity.Position);
        }

        public void AddPlayer(long userId, Entity player)
        {
            Add(player);
            byUser[userId] = player;
        }

        public void RemovePlayer(long userId)
        {
            if (!byUser.TryGetValue(userId, out Entity player)) return;

            byUser.Remove(userId);
            Remove(player);

            foreach (Entity entity in all.Values)
                entity.Forget(userId);
        }

        public void Remove(Entity entity)
        {
            despawning.Add(entity);
            Log.Info("Entity {} despawned at {}", entity.Name, entity.Position);
        }

        public Entity ByUserId(long userId)
        {
            return byUser.TryGetValue(userId, out Entity player) ? player : null;
        }

        public Entity ById(Guid id)
        {
            return all.TryGetValue(id, out Entity entity) ? entity : null;
        }

        public IEnumerable<Entity> Players()
        {
            return byUser.Values;
        }

        public void ApplyInput(long userId, PlayerIntent intent)
        {
            if (byUser.TryGetValue(userId, out Entity player))
                player.Apply(intent);
        }

        public void Tick(float dt)
        {
            Settle();
            foreach (Entity entity in all.Values)
                entity.Tick(dt);
        }

        public void CollectStates(Dictionary<Guid, EntityState> into)
        {
            foreach (Entity entity in all.Values)
                into[entity.Id] = entity.State();
        }

        public void DestroyAll()
        {
            foreach (Entity entity in all.Values)
                entity.Destroy();
            all.Clear();
            byUser.Clear();
            spawning.Clear();
            despawning.Clear();
        }

        private void Settle()
        {
            foreach (Entity entity in spawning)
            {
                all[entity.Id] = entity;
                entity.MoveToScene(scene);
                Log.Info("Entity {} spawned at {}", entity.Name, entity.Position);
            }
            spawning.Clear();

            foreach (Entity entity in despawning)
            {
                all.Remove(entity.Id);
                entity.Destroy();
                Log.Info("Entity {} despawned", entity.Name);
            }
            despawning.Clear();
        }
    }
}
