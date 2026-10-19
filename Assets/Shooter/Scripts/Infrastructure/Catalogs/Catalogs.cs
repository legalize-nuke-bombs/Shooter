using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Infrastructure
{
    public static class Catalogs
    {
        private const string ResourcesFolder = "Catalogs";
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private static Dictionary<Type, Catalog> catalogsByType;

        public static TCatalog GetCatalog<TCatalog>() where TCatalog : Catalog
        {
            catalogsByType ??= LoadAllCatalogs();

            if (catalogsByType.TryGetValue(typeof(TCatalog), out Catalog catalog)) return (TCatalog)catalog;

            Log.Error($"Catalogs serve no {typeof(TCatalog).Name}");
            return null;
        }

        private static Dictionary<Type, Catalog> LoadAllCatalogs()
        {
            var loadedCatalogsByType = new Dictionary<Type, Catalog>();

            foreach (Catalog catalog in Resources.LoadAll<Catalog>(ResourcesFolder))
            {
                if (!loadedCatalogsByType.TryAdd(catalog.GetType(), catalog))
                {
                    Log.Error($"Catalogs hold two of {catalog.GetType().Name}, {catalog.name} is ignored");
                }
            }

            Log.Info($"Catalogs loaded {loadedCatalogsByType.Count} catalogs");
            return loadedCatalogsByType;
        }
    }
}
