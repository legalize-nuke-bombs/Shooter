using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Server.Worlds.Entities;
using Shooter.Server.Worlds.Entities.Parts.Pilot;
using Shooter.Server.Worlds.Entities.Creating;
using Shooter.Server.Worlds.Time;
using Shooter.Server.Worlds.Sleeping;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities.Parts.Health;
using Shooter.Server.Worlds.Entities.Parts.Inventory;
using Shooter.Server.Worlds.Entities.Parts.Nameable;
using Shooter.Server.Worlds.Entities.Parts.Talker.AITalker.Gemini;

namespace Shooter.Server.Worlds
{
    public class ServerWorld
    {
        public string Id { get; }

        private readonly Scene scene;
        private readonly Clock clock = new Clock();
        private readonly WorldEntities entities;
        private readonly Sleep sleep;
        private readonly Sight sight;

        public ServerWorld(string id)
        {
            Id = id;
            scene = SceneManager.LoadScene("Map", new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D));
            Log.Info("World {} built: additive physics copy of Map, scene handle {}", id, scene.handle);
            sight = new Sight(scene.GetPhysicsScene());
            entities = new WorldEntities(scene);
            sleep = new Sleep(clock, entities);

            {
                var npc = new Entity("Npc", new Vector3(0f, 1.1f, 16f));
                npc.Body.AddComponent<CapsuleCollider>();

                npc.Add(new Nameable(NameableType.Kapsul));
                npc.Add(new DefaultHealth(100));
                npc.Add(new Inventory());
                npc.Add(new GeminiTalker(npc.Id, this, "Тебя зовут Капсул. Ты первый NPC добавленный в игру. Ты дружелюбный и эмпатичный. Ты помогаешь игроку.", GeminiModel.Flash35));

                entities.Add(npc);
            }
            {
                var npc = new Entity("Npc", new Vector3(5f, 1.1f, 16f));
                npc.Body.AddComponent<CapsuleCollider>();

                npc.Add(new Nameable(NameableType.SpecialCorrupted));
                npc.Add(new DefaultHealth(100));
                npc.Add(new Inventory());

                entities.Add(npc);
            }
        }

        public void Destroy()
        {
            entities.DestroyAll();
            SceneManager.UnloadSceneAsync(scene);
            Log.Info("World {} destroyed, scene unloaded", Id);
        }

        public Clock Clock()
        {
            return clock;
        }

        public int Online()
        {
            return entities.PlayerCount;
        }

        public Guid AddPlayer(long userId, string displayName)
        {
            Entity player = PlayerCreator.Create(userId, displayName, sight, clock, entities);
            entities.AddPlayer(userId, player);
            return player.Id;
        }

        public void RemovePlayer(long userId)
        {
            entities.RemovePlayer(userId);
        }

        public Entity EntityById(Guid id)
        {
            return entities.ById(id);
        }

        public Entity EntityByUserId(long userId)
        {
            return entities.ByUserId(userId);
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
