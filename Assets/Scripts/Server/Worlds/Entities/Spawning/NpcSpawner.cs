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
        public static Entity Spawn(NameableType nameableType, string nameablePayload, Vector3 position, Scene scene)
        {
            Guid id = Guid.NewGuid();

            Log.Info("Spawning npc {}...", id);

            var body = new GameObject("Npc_" + id);
            body.transform.position = position;
            body.AddComponent<CapsuleCollider>();
            SceneManager.MoveGameObjectToScene(body, scene);

            var npc = new Entity(id, body);
            npc.Add(new Nameable(nameableType, nameablePayload));
            npc.Add(new DefaultHealth(100));
            npc.Add(new Inventory());
            EntityBody.Bind(body, npc.Id);

            Log.Info("Npc spawned as entity {} at {}", npc.Id, position);
            return npc;
        }
    }
}
