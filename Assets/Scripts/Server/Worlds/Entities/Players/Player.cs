using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities.Chronology;
using Shooter.Server.Worlds.Entities.Parts;
using Shooter.Server.Worlds.Utils.Inventories;
using Shooter.Server.Worlds.Utils.Items;
using Shooter.Server.Worlds.Utils.Items.Firearm;

namespace Shooter.Server.Worlds.Entities.Players
{
    public static class Player
    {
        private const int MaxHp = 1000;

        public static Entity Spawn(long userId, string displayName, Scene scene, Clock clock, Worlds.Players worldPlayers)
        {
            var body = new GameObject("Player_" + userId);
            float angle = (userId * 137f) % 360f;
            Vector3 spread = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 16f;
            body.transform.position = new Vector3(spread.x, 1.1f, spread.z);
            CharacterController controller = body.AddComponent<CharacterController>();
            SceneManager.MoveGameObjectToScene(body, scene);

            var player = new Entity(userId, body);
            player.Add(new Nameable(displayName));
            player.Add(new Health(MaxHp));

            var inventory = new Inventory();
            inventory.Add(StackableItem.Currency, 1000);
            inventory.Add(StackableItem.Ammo762X39, 100);
            inventory.Add(new Ak47(0, 30));
            inventory.Equip(0);
            player.Add(inventory);

            player.Add(new Pilot(controller, clock, scene.GetPhysicsScene(), worldPlayers));
            EntityBody.Bind(body, userId);

            Log.Info("Player {} '{}' spawned at {}", userId, displayName, body.transform.position);
            return player;
        }

        public static PlayerState StateOf(Entity player)
        {
            Vector3 position = player.Body.transform.position;
            Pilot pilot = player.Get<Pilot>();
            Health health = player.Get<Health>();
            Nameable nameable = player.Get<Nameable>();
            Inventory inventory = player.Get<Inventory>();
            return new PlayerState
            {
                Id = player.Id,
                Name = nameable == null ? "" : nameable.Name,

                Hp = health == null ? 0 : health.Hp,
                MaxHp = health == null ? 0 : health.MaxHp,

                InventoryState = inventory == null ? null : inventory.State(),

                X = position.x,
                Y = position.y,
                Z = position.z,
                Yaw = player.Body.transform.eulerAngles.y,
                Pitch = pilot == null ? 0f : pilot.LastInput.Pitch,

                Sleeping = pilot != null && pilot.Sleeping
            };
        }
    }
}
