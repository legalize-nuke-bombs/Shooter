using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Shooter.Infrastructure
{
    public abstract class Catalog : ScriptableObject
    {
    }

    public abstract class Catalog<TAsset> : Catalog where TAsset : Asset
    {
        public const string AssetsField = nameof(assets);
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        [SerializeField] private TAsset[] assets;
        private readonly Dictionary<FixedString32Bytes, int> indicesById = new();
        private readonly List<TAsset> orderedAssets = new();
        private readonly HashSet<FixedString32Bytes> reportedUnknownIds = new();

        public int Count => orderedAssets.Count;

        protected virtual void OnEnable()
        {
            indicesById.Clear();
            orderedAssets.Clear();
            reportedUnknownIds.Clear();

            if (assets == null) return;

            foreach (TAsset asset in assets)
            {
                if (asset == null) continue;

                if (!asset.DoesIdFitNetwork())
                {
                    Log.Error($"Catalog {name} skips {asset.name}: its id does not fit the network format");
                    continue;
                }

                if (indicesById.TryGetValue(asset.NetworkId, out int takenIndex))
                {
                    Log.Error($"Catalog {name} holds both {orderedAssets[takenIndex].name} and {asset.name} under id {asset.Id}");
                    continue;
                }

                indicesById.Add(asset.NetworkId, orderedAssets.Count);
                orderedAssets.Add(asset);
            }

            Log.Info($"Catalog {name} knows {orderedAssets.Count} things");
        }

        public TAsset GetAssetAt(int index)
        {
            return index >= 0 && index < orderedAssets.Count ? orderedAssets[index] : null;
        }

        public int IndexOf(TAsset asset)
        {
            return asset != null && indicesById.TryGetValue(asset.NetworkId, out int index) ? index : -1;
        }

        public TAsset GetAssetById(FixedString32Bytes id)
        {
            if (indicesById.TryGetValue(id, out int index)) return orderedAssets[index];

            if (reportedUnknownIds.Add(id))
            {
                Log.Warn($"Catalog {name} has nothing under id {id}");
            }

            return null;
        }

        public List<TAsset> FindAll(Predicate<TAsset> predicate)
        {
            return orderedAssets.FindAll(predicate);
        }
    }
}
