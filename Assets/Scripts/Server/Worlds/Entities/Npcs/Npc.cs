using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities.Parts;
using Shooter.Server.Worlds.Utils.Inventories;

namespace Shooter.Server.Worlds.Entities.Npcs
{
    public static class Npc
    {
        private const int MaxHp = 1000;

        public static Entity Spawn(long id, string name, Vector3 position, Scene scene)
        {
            var body = new GameObject("Npc_" + id);
            body.transform.position = position;
            SceneManager.MoveGameObjectToScene(body, scene);

            var npc = new Entity(id, body);
            npc.Add(new Nameable(name));
            npc.Add(new Health(MaxHp));
            npc.Add(new Inventory());
            EntityBody.Bind(body, id);

            Log.Info("Npc {} '{}' spawned at {}", id, name, position);
            return npc;
        }

        public static NpcState StateOf(Entity npc)
        {
            Vector3 position = npc.Body.transform.position;
            Health health = npc.Get<Health>();
            Nameable nameable = npc.Get<Nameable>();
            Inventory inventory = npc.Get<Inventory>();
            return new NpcState
            {
                Id = npc.Id,
                Name = nameable == null ? "" : nameable.Name,
                Alive = health == null || health.Alive,
                InventoryState = inventory == null ? null : inventory.State(),
                X = position.x,
                Y = position.y,
                Z = position.z,
                Yaw = npc.Body.transform.eulerAngles.y
            };
        }
    }
}
