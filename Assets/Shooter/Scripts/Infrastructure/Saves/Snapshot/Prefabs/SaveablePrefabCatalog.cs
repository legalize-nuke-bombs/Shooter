using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [CreateAssetMenu(menuName = "Shooter-saves/Saveable Prefab Catalog", fileName = "SaveablePrefabCatalog")]
    public class SaveablePrefabCatalog : Catalog<SaveablePrefabAsset>
    {
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<uint, FixedString32Bytes> prefabIdsByHash = new();

        protected override void OnEnable()
        {
            base.OnEnable();
            prefabIdsByHash.Clear();
            for (int i = 0; i < Count; i++)
            {
                SaveablePrefabAsset asset = GetAssetAt(i);
                if (asset.Prefab == null)
                {
                    Log.Error($"Catalog {name} holds {asset.name} without a prefab");
                    continue;
                }

                uint hash = asset.Prefab.GetComponent<NetworkObject>().PrefabIdHash;
                if (!prefabIdsByHash.TryAdd(hash, asset.NetworkId))
                {
                    Log.Error($"Catalog {name} maps hash {hash} twice");
                }
            }
        }

        public FixedString32Bytes GetPrefabIdByHash(uint hash)
        {
            if (prefabIdsByHash.TryGetValue(hash, out FixedString32Bytes prefabId)) return prefabId;

            Log.Warn($"Failed to find prefab hash {hash}");
            return null;
        }
    }
}
