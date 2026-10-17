using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [CreateAssetMenu(menuName = "Shooter-saves/Saveable Prefab Catalog", fileName = "SaveablePrefabCatalog")]
    public class SaveablePrefabCatalog : Catalog<SaveablePrefabSpec>
    {
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<uint, FixedString32Bytes> ids = new();

        protected override void OnEnable()
        {
            base.OnEnable();
            ids.Clear();
            for (int i = 0; i < Count; i++)
            {
                SaveablePrefabSpec spec = At(i);
                if (spec.Prefab == null)
                {
                    Log.Warn($"Catalog {name} contains null spec at index {i}");
                    continue;
                }
                uint hash = spec.Prefab.GetComponent<NetworkObject>().PrefabIdHash;
                if (!ids.TryAdd(hash, spec.Id))
                {
                    Log.Error($"Catalog {name} maps hash {hash} twice");
                }
            }
        }

        public FixedString32Bytes PrefabId(uint hash)
        {
            if (ids.TryGetValue(hash, out FixedString32Bytes id))
            {
                return id;
            }
            Log.Warn($"Failed to find prefab hash {hash}");
            return null;
        }
    }
}
