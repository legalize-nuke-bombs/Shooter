using Shooter.Logging;
using Shooter.Server.Worlds.Entities.Parts.Health;
using Shooter.Server.Worlds.Entities.Parts.Inventory;
using Shooter.Server.Worlds.Entities.Parts.Movement;
using Shooter.Server.Worlds.Entities.Parts.Nameable;

namespace Shooter.Server.Worlds.Entities.Creating
{
    public static class CorpseCreator
    {
        public static Entity Create(Entity source)
        {
            var corpse = new Entity("Corpse", source.Position);

            corpse.Add(new Movement(corpse));
            corpse.Add(new Nameable(corpse, NameableType.SpecialDeadPlayer));
            corpse.Add(new DeadHealth(corpse));

            var inventory = new Inventory(corpse);
            corpse.Add(inventory);
            source.Get<Inventory>()?.DrainInto(inventory);

            Log.Info("Corpse of entity {} created at {}", source.Name, corpse.Position);
            return corpse;
        }
    }
}
