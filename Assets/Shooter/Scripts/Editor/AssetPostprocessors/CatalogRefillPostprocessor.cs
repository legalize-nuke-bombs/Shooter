using System;
using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Shooter.Editor
{
    public class CatalogRefillPostprocessor : AssetPostprocessor
    {
        private static readonly Journal Log = Logs.Here();

        private static void OnPostprocessAllAssets(string[] importedPaths, string[] deletedPaths, string[] movedPaths, string[] movedFromPaths)
        {
            if (!importedPaths.Concat(deletedPaths).Concat(movedPaths).Any(path => path.EndsWith(".asset"))) return;

            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(Catalog)))
            {
                Catalog catalog = AssetDatabase.LoadAssetAtPath<Catalog>(AssetDatabase.GUIDToAssetPath(guid));
                Type assetType = FindAssetTypeOfCatalog(catalog.GetType());
                if (assetType != null)
                {
                    RefillWithEveryAssetInProject(catalog, assetType);
                }
            }
        }

        private static Type FindAssetTypeOfCatalog(Type catalogType)
        {
            for (Type type = catalogType; type != null; type = type.BaseType)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Catalog<>))
                {
                    return type.GetGenericArguments()[0];
                }
            }

            return null;
        }

        private static void RefillWithEveryAssetInProject(Catalog catalog, Type assetType)
        {
            var assetsInProject = AssetDatabase.FindAssets("t:" + assetType.Name)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>)
                .Where(assetType.IsInstanceOfType)
                .OrderBy(asset => asset.name, StringComparer.Ordinal)
                .ToList();

            SerializedObject serializedCatalog = new(catalog);
            SerializedProperty assetsProperty = serializedCatalog.FindProperty(Catalog<Asset>.AssetsField);

            List<string> namesBefore = GetAssetNames(assetsProperty);
            var namesAfter = assetsInProject.Select(asset => asset.name).ToList();

            if (namesBefore.SequenceEqual(namesAfter)) return;

            assetsProperty.arraySize = assetsInProject.Count;
            for (int i = 0; i < assetsInProject.Count; i++)
            {
                assetsProperty.GetArrayElementAtIndex(i).objectReferenceValue = assetsInProject[i];
            }

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(catalog);

            Log.Info($"Catalog {catalog.name} refilled with every {assetType.Name} in the project: was [{string.Join(", ", namesBefore)}], now [{string.Join(", ", namesAfter)}]");
        }

        private static List<string> GetAssetNames(SerializedProperty assetsProperty)
        {
            var names = new List<string>();
            for (int i = 0; i < assetsProperty.arraySize; i++)
            {
                Object asset = assetsProperty.GetArrayElementAtIndex(i).objectReferenceValue;
                names.Add(asset == null ? "—" : asset.name);
            }

            return names;
        }
    }
}
