using UnityEngine;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities.Parts.Footsteps;
using Shooter.Server.Worlds.Entities.Parts.Hands;
using Shooter.Server.Worlds.Entities.Parts.Health;
using Shooter.Server.Worlds.Entities.Parts.Inventory;
using Shooter.Server.Worlds.Entities.Parts.Movement;
using Shooter.Server.Worlds.Entities.Parts.Nameable;
using Shooter.Server.Worlds.Entities.Parts.Pilot;
using Shooter.Server.Worlds.Entities.Parts.Sleeper;
using Shooter.Server.Worlds.Entities.Parts.Speaker;
using Shooter.Server.Worlds.Items;
using Shooter.Server.Worlds.Items.Firearm;
using Shooter.Server.Worlds.Time;

namespace Shooter.Server.Worlds.Entities.Creating
{
    public static class PlayerCreator
    {
        private const float SpawnRadius = 16f;
        private const float SpawnHeight = 1.1f;
        private const int StartHp = 100;

        public static Entity Create(long userId, string displayName, Gaze gaze, Clock clock, WorldEntities worldEntities)
        {
            Log.Info("Creating player {} '{}'...", userId, displayName);

            float angle = (userId * 137f) % 360f;
            Vector3 spread = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * SpawnRadius;

            var player = new Entity("Player", new Vector3(spread.x, SpawnHeight, spread.z));

            player.Add(new Movement(player));
            player.Add(new Footsteps(player));
            player.Add(new GivenName(player, displayName));
            player.Add(new DefaultHealth(player, StartHp));
            player.Add(new Speaker(player));
            player.Add(new Hands(player));

            var inventory = new Inventory(player);
            inventory.Add(StackableItem.Currency, 1000);
            inventory.Add(StackableItem.Ammo762X39, 100);
            inventory.Add(new Ak47(0, 30));
            inventory.TryEquip(0);
            player.Add(inventory);

            player.Add(new Parts.Shooter.Shooter(player, gaze));
            player.Add(new Sleeper(player, clock, gaze));
            player.Add(new Pilot(player, userId, gaze, worldEntities));

            Log.Info("Player {} '{}' created as entity {} at {}", userId, displayName, player.Id, player.Position);
            return player;
        }
    }
}
