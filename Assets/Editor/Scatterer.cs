using Shooter.Game.World;
using Shooter.Logging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Random = System.Random;

namespace Shooter.Editing
{
    public static class Scatterer
    {
        private const string Turn = "Tools/Scatter Rotation";
        private const string Reshuffle = "Tools/Scatter Rotation Anew";

        private const float Grow = 0.1f;
        private static readonly Journal Log = Logs.Here();

        [MenuItem(Turn)]
        private static void Keep()
        {
            Scatter(false);
        }

        [MenuItem(Reshuffle)]
        private static void Anew()
        {
            Scatter(true);
        }

        private static void Scatter(bool everyone)
        {
            Scattered[] scattered = Object.FindObjectsByType<Scattered>();
            if (scattered.Length == 0)
            {
                Log.Warn("Open scene holds nothing marked as scattered");
                return;
            }

            int scaled = 0;
            int left = 0;

            foreach (Scattered mark in scattered)
            {
                Transform standing = mark.transform;
                Transform source = Source(standing);

                if (!everyone && Touched(standing, source))
                {
                    left++;
                    continue;
                }

                Undo.RecordObject(standing, "Scatter Rotation");

                var dice = new Random(Seed(standing.position, everyone));
                Vector3 angles = source == null ? standing.eulerAngles : source.eulerAngles;
                angles.y = (float)dice.NextDouble() * 360f;
                standing.eulerAngles = angles;
                standing.localScale = Base(standing, source) * (1f + Spread(dice, Grow));

                PrefabUtility.RecordPrefabInstancePropertyModifications(standing);
                scaled++;
            }

            if (scaled > 0) EditorSceneManager.MarkAllScenesDirty();

            Log.Warn($"Scattered {scaled} of {scattered.Length} marked objects, {left} were placed by hand already");
        }

        private static Transform Source(Transform standing)
        {
            GameObject origin = PrefabUtility.GetCorrespondingObjectFromSource(standing.gameObject);

            return origin == null ? null : origin.transform;
        }

        private static Vector3 Base(Transform standing, Transform source)
        {
            return source == null ? standing.localScale : source.localScale;
        }

        private static bool Touched(Transform standing, Transform source)
        {
            if (source == null) return false;

            return !Mathf.Approximately(standing.eulerAngles.y, source.eulerAngles.y)
                   || !Mathf.Approximately(standing.localScale.x, source.localScale.x);
        }

        private static float Spread(Random dice, float reach)
        {
            return (float)(dice.NextDouble() * 2d - 1d) * reach;
        }

        private static int Seed(Vector3 at, bool everyone)
        {
            return (Mathf.RoundToInt(at.x * 73.13f) * 73856093)
                   ^ (Mathf.RoundToInt(at.y * 73.13f) * 19349663)
                   ^ (Mathf.RoundToInt(at.z * 73.13f) * 83492791)
                   ^ (everyone ? 1442695040 : 0);
        }
    }
}
