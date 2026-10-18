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

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (!imported.Concat(deleted).Concat(moved).Any(path => path.EndsWith(".asset"))) return;

            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(Catalog)))
            {
                Catalog catalog = AssetDatabase.LoadAssetAtPath<Catalog>(AssetDatabase.GUIDToAssetPath(guid));
                Type specType = SpecTypeOf(catalog.GetType());
                if (specType != null)
                {
                    Refill(catalog, specType);
                }
            }
        }

        private static Type SpecTypeOf(Type catalogType)
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

        private static void Refill(Catalog catalog, Type specType)
        {
            var specsInProject = AssetDatabase.FindAssets("t:" + specType.Name)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>)
                .Where(specType.IsInstanceOfType)
                .OrderBy(spec => spec.name, StringComparer.Ordinal)
                .ToList();

            var serializedCatalog = new SerializedObject(catalog);
            SerializedProperty specsProperty = serializedCatalog.FindProperty(Catalog<Spec>.SpecsField);

            List<string> namesBefore = SpecNames(specsProperty);
            var namesAfter = specsInProject.Select(spec => spec.name).ToList();

            if (namesBefore.SequenceEqual(namesAfter)) return;

            specsProperty.arraySize = specsInProject.Count;
            for (int i = 0; i < specsInProject.Count; i++)
            {
                specsProperty.GetArrayElementAtIndex(i).objectReferenceValue = specsInProject[i];
            }

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(catalog);

            Log.Info($"Catalog {catalog.name} refilled with every {specType.Name} in the project: was [{string.Join(", ", namesBefore)}], now [{string.Join(", ", namesAfter)}]");
        }

        private static List<string> SpecNames(SerializedProperty specsProperty)
        {
            var names = new List<string>();
            for (int i = 0; i < specsProperty.arraySize; i++)
            {
                Object spec = specsProperty.GetArrayElementAtIndex(i).objectReferenceValue;
                names.Add(spec == null ? "—" : spec.name);
            }

            return names;
        }
    }
}
