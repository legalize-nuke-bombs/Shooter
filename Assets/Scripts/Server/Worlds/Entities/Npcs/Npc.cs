using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities.Parts.Health;
using Shooter.Server.Worlds.Entities.Parts.Nameable;
using Shooter.Server.Worlds.Utils.Inventories;

namespace Shooter.Server.Worlds.Entities.Npcs
{
    public static class Npc
    {
        private const int MaxHp = 1000;

        public static Entity Spawn(string name, Vector3 position, Scene scene)
        {
            var body = new GameObject("Npc_" + name);
            body.transform.position = position;
            SceneManager.MoveGameObjectToScene(body, scene);

            var npc = new Entity(Guid.NewGuid(), body);
            npc.Add(new DefaultNameable(name));
            npc.Add(new DefaultHealth(MaxHp));
            npc.Add(new Inventory());
            EntityBody.Bind(body, npc.Id);

            Log.Info("Npc '{}' spawned as entity {} at {}", name, npc.Id, position);
            return npc;
        }
    }
}
