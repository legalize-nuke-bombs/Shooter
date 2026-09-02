using UnityEngine;

namespace Shooter.Game.World
{
    public static class Cardinal
    {
        private static readonly string[] Sides =
        {
            "north", "north-east", "east", "south-east", "south", "south-west", "west", "north-west"
        };

        public static readonly string Listed = string.Join(", ", Sides);

        public static string Side(float yaw)
        {
            return Sides[Mathf.RoundToInt(yaw / 45f) & 7];
        }

        public static string Side(Vector3 direction)
        {
            return Side(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg);
        }

        public static bool TryYaw(string side, out float yaw)
        {
            string wanted = Plain(side);

            for (int i = 0; i < Sides.Length; i++)
            {
                if (Plain(Sides[i]) != wanted) continue;

                yaw = i * 45f;
                return true;
            }

            yaw = 0f;
            return false;
        }

        private static string Plain(string side)
        {
            return (side ?? "").Trim().ToLowerInvariant().Replace("-", "").Replace(" ", "");
        }
    }
}
