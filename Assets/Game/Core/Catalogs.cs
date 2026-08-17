using System;
using System.Collections.Generic;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core
{
    [DefaultExecutionOrder(-110)]
    public class Catalogs : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private Catalog[] catalogs;

        private readonly Dictionary<Type, Catalog> known = new();

        public static Catalogs Current { get; private set; }

        private void Awake()
        {
            foreach (Catalog catalog in catalogs)
            {
                if (catalog == null) continue;

                if (!known.TryAdd(catalog.GetType(), catalog))
                    Log.Error($"Catalogs holds two of {catalog.GetType().Name}, {catalog.name} is ignored");
            }

            Log.Info($"Catalogs serve {known.Count} kinds");

            Current = this;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        public static TCatalog Of<TCatalog>() where TCatalog : Catalog
        {
            if (Current == null) return null;

            if (Current.known.TryGetValue(typeof(TCatalog), out Catalog catalog)) return (TCatalog)catalog;

            Log.Error($"Catalogs serve no {typeof(TCatalog).Name}");
            return null;
        }
    }
}
