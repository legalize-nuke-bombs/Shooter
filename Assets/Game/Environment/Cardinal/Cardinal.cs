using UnityEngine;

namespace Shooter.Game
{
    public static class Cardinal
    {
        private static readonly string[] Sides =
        {
            "север", "северо-восток", "восток", "юго-восток", "юг", "юго-запад", "запад", "северо-запад"
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
