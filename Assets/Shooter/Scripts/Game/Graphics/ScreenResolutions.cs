using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Shooter.Game
{
    public static class ScreenResolutions
    {
        private const float AspectTolerance = 0.005f;

        // In the editor Display reports 640x480, so the native size is the largest one any source knows
        public static Vector2Int GetNative()
        {
            Resolution desktop = Screen.currentResolution;
            return Screen.resolutions
                .Select(mode => new Vector2Int(mode.width, mode.height))
                .Append(new Vector2Int(desktop.width, desktop.height))
                .Append(new Vector2Int(Display.main.systemWidth, Display.main.systemHeight))
                .OrderByDescending(size => size.x * size.y)
                .First();
        }

        public static List<string> GetKeysOfThisScreen()
        {
            Vector2Int native = GetNative();
            float nativeAspect = (float)native.x / native.y;
            return Screen.resolutions
                .Select(mode => new Vector2Int(mode.width, mode.height))
                .Append(native)
                .Where(size => size.x <= native.x && size.y <= native.y && Mathf.Abs((float)size.x / size.y - nativeAspect) < AspectTolerance)
                .Distinct()
                .OrderBy(size => size.x * size.y)
                .Select(ToKey)
                .ToList();
        }

        public static string ToKey(Vector2Int size)
        {
            return $"{size.x}x{size.y}";
        }

        public static bool TryParse(string key, out Vector2Int size)
        {
            size = default;
            string[] sides = (key ?? "").Split('x');
            if (sides.Length != 2 || !int.TryParse(sides[0], out int width) || !int.TryParse(sides[1], out int height)) return false;

            size = new Vector2Int(width, height);
            return true;
        }
    }
}
