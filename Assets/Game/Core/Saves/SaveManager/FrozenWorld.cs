using System.Collections.Generic;
using Shooter.Game.Core.GameObject;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core.Saves
{
    public class FrozenWorld
    {
        private static readonly Journal Log = Logs.Here();
        private readonly Dictionary<string, SaveableObject> saveables = new();

        private FrozenWorld()
        {
        }

        public static FrozenWorld Freeze()
        {
            var world = new FrozenWorld();
            foreach (SaveableObject saveable in Object.FindObjectsByType<SaveableObject>(FindObjectsInactive.Include))
                world.Adopt(saveable);

            Log.Info($"World is frozen: {world.saveables.Count} saveable objects went dark");
            return world;
        }

        public bool TryGet(string id, out SaveableObject target)
        {
            return saveables.TryGetValue(id, out target);
        }

        public void Adopt(SaveableObject saveable)
        {
            string id = saveable.GetComponent<GameObjectId>().Id;
            if (string.IsNullOrEmpty(id))
            {
                Log.Warn($"Saveable {saveable.name} has no id, stays awake and unknown to the save");
                return;
            }

            if (!saveables.TryAdd(id, saveable))
            {
                Log.Warn($"Saveable {saveable.name} shares id {id} with {saveables[id].name}, stays awake and unknown to the save");
                return;
            }

            saveable.gameObject.SetActive(false);
        }

        public void Thaw()
        {
            int woken = 0;
            foreach (SaveableObject saveable in saveables.Values)
            {
                if (!saveable.IsSpawned) continue;

                saveable.gameObject.SetActive(true);
                woken++;
            }

            Log.Info($"World is thawed: {woken} of {saveables.Count} saveable objects woke up");
            saveables.Clear();
        }
    }
}
