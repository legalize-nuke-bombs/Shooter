using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Shooter.Game.Body
{
    public static class Digestion
    {
        public static string Of(Component entity, DigestionDetail detail)
        {
            return Block(entity, detail, null);
        }

        public static string Seen(Component entity, DigestionDetail detail, Transform eyes)
        {
            return Block(entity, detail, eyes);
        }

        private static string Block(Component entity, DigestionDetail detail, Transform eyes)
        {
            IEnumerable<IDigestible> parts = entity.GetComponents<IDigestible>()
                .OrderByDescending(part => part.Priority);

            StringBuilder digest = new StringBuilder();

            foreach (IDigestible part in parts)
            {
                string said = part.Digest(detail);
                if (string.IsNullOrWhiteSpace(said)) continue;

                foreach (string line in said.Trim().Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (digest.Length == 0)
                    {
                        digest.Append(line.TrimEnd());
                        if (eyes != null) digest.Append(" (").Append(Whereabouts(entity, eyes)).Append(")");
                        continue;
                    }

                    digest.Append("\n  ").Append(line.TrimEnd());
                }
            }

            return digest.Length == 0 ? null : digest.ToString();
        }

        private static string Whereabouts(Component entity, Transform eyes)
        {
            Vector3 offset = entity.transform.position - eyes.position;

            return Mathf.RoundToInt(offset.magnitude) + " м, " + Cardinal.Side(offset);
        }
    }
}
