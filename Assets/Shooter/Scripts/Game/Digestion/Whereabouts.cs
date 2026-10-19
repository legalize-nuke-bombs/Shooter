using UnityEngine;

namespace Shooter.Game
{
    public static class Whereabouts
    {
        public const float ArrivalTolerance = 1f;

        public static string FormatCoordinates(Vector3 position)
        {
            return "(" + Mathf.RoundToInt(position.x) + ", " + Mathf.RoundToInt(position.y) + ", " + Mathf.RoundToInt(position.z) + ")";
        }

        public static string DescribeShortfall(Vector3 remainingOffset)
        {
            if (remainingOffset.magnitude < ArrivalTolerance) return "";

            return Mathf.RoundToInt(remainingOffset.magnitude) + " m short of the point";
        }
    }
}
