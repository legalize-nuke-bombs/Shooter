using UnityEngine;

namespace Shooter.Game
{
    public static class Celestial
    {
        public static float GetElevation(float hourAngle, float declination, float latitude)
        {
            float hourRadians = hourAngle * Mathf.Deg2Rad;
            float declinationRadians = declination * Mathf.Deg2Rad;
            float latitudeRadians = latitude * Mathf.Deg2Rad;

            float elevationSine = Mathf.Sin(latitudeRadians) * Mathf.Sin(declinationRadians)
                                  + Mathf.Cos(latitudeRadians) * Mathf.Cos(declinationRadians) * Mathf.Cos(hourRadians);

            return Mathf.Asin(Mathf.Clamp(elevationSine, -1f, 1f)) * Mathf.Rad2Deg;
        }

        public static float GetAzimuth(float hourAngle, float declination, float latitude)
        {
            float hourRadians = hourAngle * Mathf.Deg2Rad;
            float declinationRadians = declination * Mathf.Deg2Rad;
            float latitudeRadians = latitude * Mathf.Deg2Rad;

            float toEast = -Mathf.Cos(declinationRadians) * Mathf.Sin(hourRadians);
            float toNorth = Mathf.Sin(declinationRadians) * Mathf.Cos(latitudeRadians)
                            - Mathf.Cos(declinationRadians) * Mathf.Sin(latitudeRadians) * Mathf.Cos(hourRadians);

            return Mathf.Atan2(toEast, toNorth) * Mathf.Rad2Deg;
        }

        public static Quaternion GetLightRotation(float hourAngle, float declination, float latitude)
        {
            float elevation = GetElevation(hourAngle, declination, latitude);
            float azimuth = GetAzimuth(hourAngle, declination, latitude);

            return Quaternion.Euler(elevation, azimuth + 180f, 0f);
        }
    }
}
