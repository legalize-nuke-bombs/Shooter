using UnityEngine;

namespace Shooter.Game
{
    public static class Celestial
    {
        public static float Elevation(float hourAngle, float declination, float latitude)
        {
            float hourRadians = hourAngle * Mathf.Deg2Rad;
            float declinationRadians = declination * Mathf.Deg2Rad;
            float latitudeRadians = latitude * Mathf.Deg2Rad;

            float height = Mathf.Sin(latitudeRadians) * Mathf.Sin(declinationRadians)
                + Mathf.Cos(latitudeRadians) * Mathf.Cos(declinationRadians) * Mathf.Cos(hourRadians);

            return Mathf.Asin(Mathf.Clamp(height, -1f, 1f)) * Mathf.Rad2Deg;
        }

        public static float Azimuth(float hourAngle, float declination, float latitude)
        {
            float hourRadians = hourAngle * Mathf.Deg2Rad;
            float declinationRadians = declination * Mathf.Deg2Rad;
            float latitudeRadians = latitude * Mathf.Deg2Rad;

            float toEast = -Mathf.Cos(declinationRadians) * Mathf.Sin(hourRadians);
            float toNorth = Mathf.Sin(declinationRadians) * Mathf.Cos(latitudeRadians)
                - Mathf.Cos(declinationRadians) * Mathf.Sin(latitudeRadians) * Mathf.Cos(hourRadians);

            return Mathf.Atan2(toEast, toNorth) * Mathf.Rad2Deg;
        }

        public static Quaternion Rotation(float hourAngle, float declination, float latitude)
        {
            float elevation = Elevation(hourAngle, declination, latitude);
            float azimuth = Azimuth(hourAngle, declination, latitude);

            return Quaternion.Euler(elevation, azimuth + 180f, 0f);
        }

        public static float HalfDayAngle(float declination, float latitude)
        {
            float horizonCrossing = -Mathf.Tan(latitude * Mathf.Deg2Rad) * Mathf.Tan(declination * Mathf.Deg2Rad);

            return Mathf.Acos(Mathf.Clamp(horizonCrossing, -1f, 1f)) * Mathf.Rad2Deg;
        }
    }
}
