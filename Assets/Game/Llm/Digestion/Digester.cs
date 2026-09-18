using System.Text;
using Shooter.Game.Core;
using UnityEngine;

namespace Shooter.Game.Llm
{
    public static class Digester
    {
        public static string Of(GameObject entity, DigestionDetail detail)
        {
            return entity == null ? null : Of(entity.GetComponent<MainDigestible>(), detail);
        }

        public static string Of(MainDigestible entity, DigestionDetail detail)
        {
            if (entity == null) return null;

            var head = new StringBuilder();
            var body = new StringBuilder();

            foreach (IDigestible part in entity.Parts)
            {
                string said = part.Digest(detail);
                if (string.IsNullOrWhiteSpace(said)) continue;

                bool heading = part.Priority >= DigestionPriority.Place;

                foreach (string line in said.Trim().Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (heading)
                    {
                        if (head.Length > 0) head.Append(' ');
                        head.Append(line.Trim());
                        heading = false;
                        continue;
                    }

                    body.Append("\n  ").Append(line.TrimEnd());
                }
            }

            if (head.Length == 0) return null;

            return head.Append(body).ToString();
        }
    }
}
