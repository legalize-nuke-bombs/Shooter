using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Shooter.Server.Worlds.Entities;
using Shooter.Server.Worlds.Entities.Parts.Pilot;

namespace Shooter.Server.Worlds
{
    public class WorldEntities
    {
        private readonly Scene scene;
        private readonly Dictionary<Guid, Entity> all = new Dictionary<Guid, Entity>();
        private readonly Dictionary<long, Entity> byUser = new Dictionary<long, Entity>();

        public int PlayerCount => byUser.Count;

        public WorldEntities(Scene scene)
        {
            this.scene = scene;
        }

        public void Add(Entity entity)
        {
            all[entity.Id] = entity;
            SceneManager.MoveGameObjectToScene(entity.Body, scene);
        }

        public void AddPlayer(long userId, Entity player)
        {
            Add(player);
            byUser[userId] = player;
        }

        public void RemovePlayer(long userId)
        {
            if (!byUser.TryGetValue(userId, out Entity player)) return;
            all.Remove(player.Id);
            byUser.Remove(userId);
            player.Destroy();
        }

        public Entity ById(Guid id)
        {
            return all.TryGetValue(id, out Entity entity) ? entity : null;
        }

        public void ApplyInput(long userId, PlayerIntent intent)
        {
            if (byUser.TryGetValue(userId, out Entity player))
                player.Get<Pilot>()?.Apply(intent);
        }

        public bool AllAsleep()
        {
            if (byUser.Count == 0) return false;
            foreach (Entity player in byUser.Values)
            {
                Pilot pilot = player.Get<Pilot>();
                if (pilot == null || !pilot.Sleeping)
                    return false;
            }
            return true;
        }

        public void WakeAll()
        {
            foreach (Entity player in byUser.Values)
                player.Get<Pilot>()?.WakeUp();
        }

        public void Tick(float dt)
        {
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
        }
    }
}
