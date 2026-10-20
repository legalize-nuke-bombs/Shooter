using System;
using System.Collections.Generic;
using Shooter.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace Shooter.Editor
{
    public static class BakeSaveableObjectIdsTool
    {
        private static readonly Journal Log = Logs.Here();

        [MenuItem("Tools/Bake Saveable Object IDs")]
        private static void BakeMissingIds()
        {
            SaveableObject[] saveables = UnityEngine.Object.FindObjectsByType<SaveableObject>(FindObjectsInactive.Include);

            int bakedIds = 0;
            foreach (SaveableObject saveable in saveables)
            {
                if (!string.IsNullOrEmpty(saveable.Id)) continue;

                SerializedObject serializedSaveable = new(saveable);
                serializedSaveable.FindProperty(SaveableObject.IdField).stringValue = Guid.NewGuid().ToString();
                serializedSaveable.ApplyModifiedProperties();

                Log.Info($"Saveable {saveable.name} got id {saveable.Id}");
                bakedIds++;
            }

            var seenIds = new HashSet<string>();
            int repeatedIds = 0;
            foreach (SaveableObject saveable in saveables)
            {
                if (seenIds.Add(saveable.Id)) continue;

                Log.Warn($"Saveable {saveable.name} repeats the id {saveable.Id} of another one");
                repeatedIds++;
            }

            string summary = $"Baked {bakedIds} of {saveables.Length} saveable ids, {repeatedIds} duplicates";
            Log.Info(summary);
            EditorUtility.DisplayDialog("Bake Saveable Object IDs", summary, "OK");
        }
    }
}
