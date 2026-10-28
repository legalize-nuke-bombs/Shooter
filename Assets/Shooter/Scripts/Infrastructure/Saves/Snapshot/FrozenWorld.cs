using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    public class FrozenWorld
    {
        private static readonly Journal Log = Logs.Here();
        private readonly Dictionary<string, SaveableEntity> saveableEntitiesById = new();

        private FrozenWorld()
        {
        }

        public static FrozenWorld FreezeAllSaveableEntities()
        {
            FrozenWorld frozenWorld = new();
            foreach (SaveableEntity saveable in Object.FindObjectsByType<SaveableEntity>(FindObjectsInactive.Include))
            {
                frozenWorld.FreezeSaveableEntity(saveable);
            }

            Log.Info($"World is frozen: {frozenWorld.saveableEntitiesById.Count} saveable entities went dark");
            return frozenWorld;
        }

        public bool TryGetSaveableEntity(string id, out SaveableEntity saveable)
        {
            return saveableEntitiesById.TryGetValue(id, out saveable);
        }

        public void FreezeSaveableEntity(SaveableEntity saveable)
        {
            string id = saveable.Id;
            if (string.IsNullOrEmpty(id))
            {
                Log.Warn($"Saveable {saveable.name} has no id, stays awake and unknown to the save");
                return;
            }

            if (!saveableEntitiesById.TryAdd(id, saveable))
            {
                Log.Warn($"Saveable {saveable.name} shares id {id} with {saveableEntitiesById[id].name}, stays awake and unknown to the save");
                return;
            }

            saveable.gameObject.SetActive(false);
        }

        public void ThawAllSaveableEntities()
        {
            int wokenCount = 0;
            foreach (SaveableEntity saveable in saveableEntitiesById.Values)
            {
                if (!saveable.IsSpawned) continue;

                saveable.gameObject.SetActive(saveable.IsActiveAfterThaw);
                if (saveable.IsActiveAfterThaw) wokenCount++;
            }

            Log.Info($"World is thawed: {wokenCount} of {saveableEntitiesById.Count} saveable entities woke up");
            saveableEntitiesById.Clear();
        }
    }
}
