using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Game.Digesting
{
    public static class Digestion
    {
        public static string Of(Component entity)
        {
            var lines = new List<string>();

            foreach (IDigestible digestible in entity.GetComponents<IDigestible>())
            {
                string line = digestible.Digest();
                if (!string.IsNullOrEmpty(line)) lines.Add(line);
            }

            return string.Join("\n", lines);
        }
    }
}
