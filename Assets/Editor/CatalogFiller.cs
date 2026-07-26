using System;
using System.Collections.Generic;
using System.Linq;
using Shooter.Game;
using Shooter.Logging;
using UnityEditor;
using UnityEngine;

namespace Shooter.Editing
{
    public class CatalogFiller : AssetPostprocessor
    {
        private const string SpecsField = "specs";

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted,
            string[] moved, string[] movedFrom)
        {
            if (imported.Length == 0 && deleted.Length == 0 && moved.Length == 0) return;

            foreach (string path in Assets("t:ScriptableObject"))
            {
                var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (catalog == null) continue;

                Type kind = SpecKind(catalog.GetType());
                if (kind != null) Fill(catalog, kind);
            }
        }

        private static Type SpecKind(Type catalog)
        {
            for (Type type = catalog; type != null; type = type.BaseType)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Catalog<>))
                    return type.GetGenericArguments()[0];
            }

            return null;
        }

        private static void Fill(ScriptableObject catalog, Type kind)
        {
            List<ScriptableObject> found = Assets("t:" + kind.Name)
                .Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>)
                .Where(spec => spec != null && kind.IsInstanceOfType(spec))
                .OrderBy(spec => spec.name, StringComparer.Ordinal)
                .ToList();

            var serialized = new SerializedObject(catalog);
            SerializedProperty specs = serialized.FindProperty(SpecsField);

            if (specs == null)
            {
                Log.Error("Catalog {} has no {} field to fill", catalog.name, SpecsField);
                return;
            }

            List<string> before = Listed(specs);
            List<string> after = found.Select(spec => spec.name).ToList();

            if (before.SequenceEqual(after)) return;

            specs.arraySize = found.Count;
            for (int i = 0; i < found.Count; i++)
                specs.GetArrayElementAtIndex(i).objectReferenceValue = found[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(catalog);

            Log.Warn("Catalog {} refilled with every {} in the project: was [{}], now [{}]",
                catalog.name, kind.Name, string.Join(", ", before), string.Join(", ", after));
        }

        private static List<string> Listed(SerializedProperty specs)
        {
            var names = new List<string>();

            for (int i = 0; i < specs.arraySize; i++)
            {
                UnityEngine.Object spec = specs.GetArrayElementAtIndex(i).objectReferenceValue;
                names.Add(spec == null ? "—" : spec.name);
            }

            return names;
        }

        private static IEnumerable<string> Assets(string filter)
        {
            return AssetDatabase.FindAssets(filter).Select(AssetDatabase.GUIDToAssetPath).Distinct();
        }
    }
}
