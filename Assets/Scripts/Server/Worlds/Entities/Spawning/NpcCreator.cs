using UnityEngine;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities.Parts.Health;
using Shooter.Server.Worlds.Entities.Parts.Nameable;
using Shooter.Server.Worlds.Entities.Parts.Inventory;
using Shooter.Server.Worlds.Entities.Parts.Talker;

namespace Shooter.Server.Worlds.Entities.Spawning
{
    public static class NpcCreator
    {
        public static Entity Create(Nameable nameable, Health health, Inventory inventory, Talker talker, Vector3 position)
        {
            var npc = new Entity("Npc", position);
            npc.Body.AddComponent<CapsuleCollider>();

            npc.Add(nameable);
            npc.Add(health);
            npc.Add(inventory);
            if (talker != null) npc.Add(talker);

            Log.Info("Npc {} created at {}", npc.Id, position);
            return npc;
        }
    }
}
