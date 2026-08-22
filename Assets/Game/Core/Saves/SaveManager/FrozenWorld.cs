using System.Collections.Generic;
using Shooter.Game.Core.GameObject;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core.Saves
{
    public class FrozenWorld
    {
        private static readonly Journal Log = Logs.Here();
        private readonly Dictionary<string, SaveableObject> byId = new();
        private readonly List<SaveableObject> frozen = new();

        private FrozenWorld()
        {
        }

        internal static FrozenWorld Freeze()
        {
            var world = new FrozenWorld();
            foreach (SaveableObject saveable in Object.FindObjectsByType<SaveableObject>(FindObjectsInactive.Include))
            {
                world.Index(saveable);
                world.Adopt(saveable);
            }

            Log.Info($"World is frozen: {world.frozen.Count} saveable objects went dark, {world.byId.Count} answer by id");
            return world;
        }

        internal bool TryGet(string id, out SaveableObject target)
        {
            return byId.TryGetValue(id, out target);
        }

        internal void Adopt(SaveableObject saveable)
        {
            if (!saveable.gameObject.activeSelf) return;

            saveable.gameObject.SetActive(false);
            frozen.Add(saveable);
        }

        internal void Thaw()
        {
            int woken = 0;
            foreach (SaveableObject saveable in frozen)
            {
                if (saveable == null || !saveable.IsSpawned) continue;

                saveable.gameObject.SetActive(true);
                woken++;
            }

            Log.Info($"World is thawed: {woken} of {frozen.Count} saveable objects woke up");
            frozen.Clear();
        }

        private void Index(SaveableObject saveable)
        {
            string id = saveable.GetComponent<GameObjectId>().Id;
            if (string.IsNullOrEmpty(id))
            {
                Log.Warn($"Saveable {saveable.name} has no id and answers to no record");
                return;
            }

            if (!byId.TryAdd(id, saveable))
                Log.Warn($"Saveable {saveable.name} shares id {id} with {byId[id].name}, only the first answers");
        }
    }
}
