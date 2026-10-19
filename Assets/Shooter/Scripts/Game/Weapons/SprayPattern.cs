using System;
using UnityEngine;

namespace Shooter.Game
{
    [Serializable]
    public class SprayPattern
    {
        public const string PointsField = nameof(points);

        [SerializeField] private Vector2[] points = new Vector2[0];

        public int Length => points.Length;

        public Vector2 GetOffsetForShot(int shotIndex)
        {
            if (points.Length == 0) return Vector2.zero;

            return points[Mathf.Clamp(shotIndex, 0, points.Length - 1)];
        }
    }
}
