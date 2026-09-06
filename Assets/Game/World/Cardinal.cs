using UnityEngine;

namespace Shooter.Game.World
{
    public static class Cardinal
    {
        public const string Degree = "\u00B0";
        public const float ArrivalTolerance = 1f;

        private const float RiseThreshold = 2f;

        private static readonly string[] Sides =
        {
            "north", "north-east", "east", "south-east", "south", "south-west", "west", "north-west"
        };

        public static string Side(float yaw)
        {
            return Sides[Mathf.RoundToInt(yaw / 45f) & 7];
        }

        public static string Side(Vector3 direction)
        {
            return Side(Yaw(direction));
        }

        public static float Yaw(Vector3 direction)
        {
            return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }

        public static int Bearing(float yaw)
        {
            return (Mathf.RoundToInt(yaw) % 360 + 360) % 360;
        }

        public static string Whereabouts(Vector3 offset)
        {
            float yaw = Yaw(offset);
            return Mathf.RoundToInt(offset.magnitude) + " m, " + Side(yaw) + " (" + Bearing(yaw) + Degree + ")";
        }

        public static string Shortfall(Vector3 rest)
        {
            if (rest.magnitude < ArrivalTolerance) return "";

            return Mathf.RoundToInt(rest.magnitude) + " m short of the point" + Rise(rest.y);
        }

        private static string Rise(float dy)
        {
            if (Mathf.Abs(dy) < RiseThreshold) return "";

            return ", " + Mathf.RoundToInt(Mathf.Abs(dy)) + " m " + (dy > 0f ? "up" : "down");
        }
    }
}
