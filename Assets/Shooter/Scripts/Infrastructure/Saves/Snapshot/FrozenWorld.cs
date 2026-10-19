using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    public class FrozenWorld
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();
        private readonly Dictionary<string, SaveableObject> saveableObjectsById = new();

        private FrozenWorld()
        {
        }

        public static FrozenWorld FreezeAllSaveableObjects()
        {
            FrozenWorld frozenWorld = new();
            foreach (SaveableObject saveable in Object.FindObjectsByType<SaveableObject>(FindObjectsInactive.Include))
            {
                frozenWorld.FreezeSaveableObject(saveable);
            }

            Log.Info($"World is frozen: {frozenWorld.saveableObjectsById.Count} saveable objects went dark");
            return frozenWorld;
        }

        public bool TryGetSaveableObject(string id, out SaveableObject saveable)
        {
            return saveableObjectsById.TryGetValue(id, out saveable);
        }

        public void FreezeSaveableObject(SaveableObject saveable)
        {
            string id = saveable.Id;
            if (string.IsNullOrEmpty(id))
            {
                Log.Warn($"Saveable {saveable.name} has no id, stays awake and unknown to the save");
                return;
            }

            if (!saveableObjectsById.TryAdd(id, saveable))
            {
                Log.Warn($"Saveable {saveable.name} shares id {id} with {saveableObjectsById[id].name}, stays awake and unknown to the save");
                return;
            }

            saveable.gameObject.SetActive(false);
        }

        public void ThawAllSaveableObjects()
        {
            int wokenCount = 0;
            foreach (SaveableObject saveable in saveableObjectsById.Values)
            {
                if (!saveable.IsSpawned) continue;

                saveable.gameObject.SetActive(saveable.IsActiveAfterThaw);
                if (saveable.IsActiveAfterThaw) wokenCount++;
            }

            Log.Info($"World is thawed: {wokenCount} of {saveableObjectsById.Count} saveable objects woke up");
            saveableObjectsById.Clear();
        }
    }
}
