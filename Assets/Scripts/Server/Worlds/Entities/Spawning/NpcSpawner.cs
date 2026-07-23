using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities.Parts.Health;
using Shooter.Server.Worlds.Entities.Parts.Nameable;
using Shooter.Server.Worlds.Entities.Parts.Inventory;
using Shooter.Server.Worlds.Entities.Parts.Talker;

namespace Shooter.Server.Worlds.Entities.Spawning
{
    public static class NpcSpawner
    {
        public static Entity Spawn(Nameable nameable, Health health, Inventory inventory, Talker talker, Vector3 position, Scene scene)
        {
            Guid id = Guid.NewGuid();

            Log.Info("Spawning npc {}...", id);

            var body = new GameObject("Npc_" + id);
            body.transform.position = position;
            body.AddComponent<CapsuleCollider>();
            SceneManager.MoveGameObjectToScene(body, scene);

            var npc = new Entity(id, body);
            npc.Add(nameable);
            npc.Add(health);
            npc.Add(inventory);
            if (talker != null) npc.Add(talker);
            EntityBody.Bind(body, npc.Id);

            Log.Info("Npc spawned as entity {} at {}", npc.Id, position);
            return npc;
        }
    }
}
