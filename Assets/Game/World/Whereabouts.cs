using UnityEngine;

namespace Shooter.Game.World
{
    public static class Whereabouts
    {
        public const float ArrivalTolerance = 1f;

        public static string Coordinates(Vector3 position)
        {
            return "(" + Mathf.RoundToInt(position.x) + ", " + Mathf.RoundToInt(position.y) + ", " + Mathf.RoundToInt(position.z) + ")";
        }

        public static string Shortfall(Vector3 rest)
        {
            if (rest.magnitude < ArrivalTolerance) return "";

            return Mathf.RoundToInt(rest.magnitude) + " m short of the point";
        }
    }
}
