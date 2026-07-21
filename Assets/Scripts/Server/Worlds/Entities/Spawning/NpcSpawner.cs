using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities.Parts.Health;
using Shooter.Server.Worlds.Entities.Parts.Nameable;
using Shooter.Server.Worlds.Entities.Parts.Inventory;

namespace Shooter.Server.Worlds.Entities.Spawning
{
    public static class NpcSpawner
    {
        public static Entity Spawn(string name, Vector3 position, Scene scene)
        {
            Log.Info("Spawning npc...");

            var body = new GameObject("Npc_" + name);
            body.transform.position = position;
            body.AddComponent<CapsuleCollider>();
            SceneManager.MoveGameObjectToScene(body, scene);

            var npc = new Entity(Guid.NewGuid(), body);
            npc.Add(new DefaultNameable(name));
            npc.Add(new DefaultHealth(100));
            npc.Add(new Inventory());
            EntityBody.Bind(body, npc.Id);

            Log.Info("Npc '{}' spawned as entity {} at {}", name, npc.Id, position);
            return npc;
        }
    }
}
