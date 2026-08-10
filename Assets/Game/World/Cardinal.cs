using UnityEngine;

namespace Shooter.Game.World
{
    public static class Cardinal
    {
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
            return Side(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg);
        }
    }
}
