using System;
using System.Collections.Generic;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core
{
    public static class Catalogs
    {
        private const string Folder = "Catalogs";
        private static readonly Journal Log = Logs.Here();

        private static Dictionary<Type, Catalog> known;

        public static TCatalog Of<TCatalog>() where TCatalog : Catalog
        {
            known ??= Load();

            if (known.TryGetValue(typeof(TCatalog), out Catalog catalog)) return (TCatalog)catalog;

            Log.Error($"Catalogs serve no {typeof(TCatalog).Name}");
            return null;
        }

        private static Dictionary<Type, Catalog> Load()
        {
            var found = new Dictionary<Type, Catalog>();

            foreach (Catalog catalog in Resources.LoadAll<Catalog>(Folder))
            {
                if (!found.TryAdd(catalog.GetType(), catalog))
                    Log.Error($"Catalogs hold two of {catalog.GetType().Name}, {catalog.name} is ignored");
            }

            Log.Info($"Catalogs serve {found.Count} kinds");
            return found;
        }
    }
}
