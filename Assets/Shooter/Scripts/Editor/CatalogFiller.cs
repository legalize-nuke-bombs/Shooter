using System;
using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Shooter.Editor
{
    public class CatalogFiller : AssetPostprocessor
    {
        private static readonly Journal Log = Logs.Here();

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (!imported.Concat(deleted).Concat(moved).Any(path => path.EndsWith(".asset"))) return;

            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(Catalog)))
            {
                var catalog = AssetDatabase.LoadAssetAtPath<Catalog>(AssetDatabase.GUIDToAssetPath(guid));
                Type specType = SpecType(catalog.GetType());
                if (specType != null)
                {
                    Fill(catalog, specType);
                }
            }
        }

        private static Type SpecType(Type catalog)
        {
            for (Type type = catalog; type != null; type = type.BaseType)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Catalog<>))
                {
                    return type.GetGenericArguments()[0];
                }
            }

            return null;
        }

        private static void Fill(Catalog catalog, Type specType)
        {
            var found = AssetDatabase.FindAssets("t:" + specType.Name)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>)
                .Where(specType.IsInstanceOfType)
                .OrderBy(spec => spec.name, StringComparer.Ordinal)
                .ToList();

            var serialized = new SerializedObject(catalog);
            SerializedProperty specs = serialized.FindProperty(Catalog<Spec>.SpecsField);

            List<string> before = Listed(specs);
            var after = found.Select(spec => spec.name).ToList();

            if (before.SequenceEqual(after)) return;

            specs.arraySize = found.Count;
            for (int i = 0; i < found.Count; i++)
            {
                specs.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(catalog);

            Log.Info($"Catalog {catalog.name} refilled with every {specType.Name} in the project: was [{string.Join(", ", before)}], now [{string.Join(", ", after)}]");
        }

        private static List<string> Listed(SerializedProperty specs)
        {
            var names = new List<string>();
            for (int i = 0; i < specs.arraySize; i++)
            {
                Object spec = specs.GetArrayElementAtIndex(i).objectReferenceValue;
                names.Add(spec == null ? "—" : spec.name);
            }

            return names;
        }
    }
}
