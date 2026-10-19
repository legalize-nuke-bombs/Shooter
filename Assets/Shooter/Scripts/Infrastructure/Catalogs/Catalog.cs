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
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private TAsset[] assets;
        private readonly Dictionary<FixedString32Bytes, int> indices = new();
        private readonly List<TAsset> ordered = new();
        private readonly HashSet<FixedString32Bytes> reportedUnknown = new();

        public int Count => ordered.Count;

        protected virtual void OnEnable()
        {
            indices.Clear();
            ordered.Clear();
            reportedUnknown.Clear();

            if (assets == null) return;

            foreach (TAsset asset in assets)
            {
                if (asset == null) continue;

                if (!asset.IdFitsNetwork())
                {
                    Log.Error($"Catalog {name} skips {asset.name}: its id does not fit the network format");
                    continue;
                }

                if (indices.TryGetValue(asset.NetworkId, out int taken))
                {
                    Log.Error($"Catalog {name} holds both {ordered[taken].name} and {asset.name} under id {asset.Id}");
                    continue;
                }

                indices.Add(asset.NetworkId, ordered.Count);
                ordered.Add(asset);
            }

            Log.Info($"Catalog {name} knows {ordered.Count} things");
        }

        public TAsset At(int index)
        {
            return index >= 0 && index < ordered.Count ? ordered[index] : null;
        }

        public int Index(TAsset asset)
        {
            return asset != null && indices.TryGetValue(asset.NetworkId, out int index) ? index : -1;
        }

        public TAsset Of(FixedString32Bytes id)
        {
            if (indices.TryGetValue(id, out int index)) return ordered[index];

            if (reportedUnknown.Add(id))
            {
                Log.Warn($"Catalog {name} has nothing under id {id}");
            }

            return null;
        }

        public List<TAsset> FindAll(Predicate<TAsset> predicate)
        {
            return ordered.FindAll(predicate);
        }
    }
}
