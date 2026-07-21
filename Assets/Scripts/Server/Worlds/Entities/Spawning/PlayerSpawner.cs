using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Logging;
using Shooter.Server.Worlds.Time;
using Shooter.Server.Worlds.Entities.Parts.Pilot;
using Shooter.Server.Worlds.Entities.Parts.Health;
using Shooter.Server.Worlds.Entities.Parts.Nameable;
using Shooter.Server.Worlds.Entities.Parts.Inventory;
using Shooter.Server.Worlds.Entities.Parts.Speaker;
using Shooter.Server.Worlds.Entities.Parts.Shooter;
using Shooter.Server.Worlds.Entities.Parts.Hands;
using Shooter.Server.Worlds.Items;
using Shooter.Server.Worlds.Items.Firearm;

namespace Shooter.Server.Worlds.Entities.Spawning
{
    public static class PlayerSpawner
    {
        public static Entity Spawn(long userId, string displayName, Scene scene, Sight sight, Clock clock, Worlds.WorldEntities worldEntities)
        {
            Log.Info("Spawning Player {} '{}'...", userId, displayName);

            var body = new GameObject("Player_" + userId);
            float angle = (userId * 137f) % 360f;
            Vector3 spread = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 16f;
            body.transform.position = new Vector3(spread.x, 1.1f, spread.z);
            CharacterController controller = body.AddComponent<CharacterController>();
            SceneManager.MoveGameObjectToScene(body, scene);

            var player = new Entity(Guid.NewGuid(), body);
            player.Add(new DefaultNameable(displayName));
            player.Add(new DefaultHealth(100));

            var inventory = new Inventory();
            inventory.Add(StackableItem.Currency, 1000);
            inventory.Add(StackableItem.Ammo762X39, 100);
            inventory.Add(new Ak47(0, 30));
            inventory.Equip(0);
            player.Add(inventory);

            var speaker = new Speaker();
            player.Add(speaker);

            var hands = new Hands();
            player.Add(hands);

            var shooter = new Parts.Shooter.Shooter(inventory, speaker, sight, worldEntities, hands);
            player.Add(shooter);

            player.Add(new Pilot(controller, speaker, shooter, hands, clock, sight, worldEntities));
            EntityBody.Bind(body, player.Id);

            Log.Info("Player {} '{}' spawned as entity {} at {}", userId, displayName, player.Id, body.transform.position);
            return player;
        }
    }
}
