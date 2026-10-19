using System;
using System.Collections.Generic;
using Shooter.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace Shooter.Editor
{
    public static class BakeSaveableObjectIdsTool
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        [MenuItem("Tools/Bake Saveable Object IDs")]
        private static void BakeIds()
        {
            SaveableObject[] saveables = UnityEngine.Object.FindObjectsByType<SaveableObject>(FindObjectsInactive.Include);

            int baked = 0;
            foreach (SaveableObject saveable in saveables)
            {
                if (!string.IsNullOrEmpty(saveable.Id)) continue;

                SerializedObject serialized = new(saveable);
                serialized.FindProperty(SaveableObject.IdField).stringValue = Guid.NewGuid().ToString();
                serialized.ApplyModifiedProperties();

                Log.Info($"Saveable {saveable.name} got id {saveable.Id}");
                baked++;
            }

            var seen = new HashSet<string>();
            int duplicates = 0;
            foreach (SaveableObject saveable in saveables)
            {
                if (seen.Add(saveable.Id)) continue;

                Log.Warn($"Saveable {saveable.name} repeats the id {saveable.Id} of another one");
                duplicates++;
            }

            string summary = $"Baked {baked} of {saveables.Length} saveable ids, {duplicates} duplicates";
            Log.Info(summary);
            EditorUtility.DisplayDialog("Bake Saveable Object IDs", summary, "OK");
        }
    }
}
