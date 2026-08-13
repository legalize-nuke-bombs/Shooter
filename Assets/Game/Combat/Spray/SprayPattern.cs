using System;
using UnityEngine;

namespace Shooter.Game.Combat
{
    [Serializable]
    public class SprayPattern
    {
        [SerializeField] private Vector2[] points = new Vector2[0];

        public int Length => points.Length;

        public Vector2 At(int shot)
        {
            if (points.Length == 0) return Vector2.zero;

            return points[Mathf.Clamp(shot, 0, points.Length - 1)];
        }
    }
}
