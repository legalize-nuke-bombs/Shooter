using System.Text;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public static class Digester
    {
        public static string DigestEntity(GameObject entity, DigestionDetail detail)
        {
            return entity == null ? null : DigestEntity(entity.GetComponent<MainDigestible>(), detail);
        }

        public static string DigestEntity(MainDigestible mainDigestible, DigestionDetail detail)
        {
            if (mainDigestible == null) return null;

            StringBuilder headline = new();
            StringBuilder details = new();

            IncrementalId incrementalId = mainDigestible.GetComponent<IncrementalId>();
            if (incrementalId != null) headline.Append("[ID ").Append(incrementalId.Value).Append(']');

            foreach (IDigestible part in mainDigestible.Parts)
            {
                string partDigest = part.Digest(detail);
                if (string.IsNullOrWhiteSpace(partDigest)) continue;

                bool isNextLineHeadline = part.Priority >= DigestionPriority.Place;

                foreach (string line in partDigest.Trim().Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (isNextLineHeadline)
                    {
                        if (headline.Length > 0) headline.Append(' ');
                        headline.Append(line.Trim());
                        isNextLineHeadline = false;
                        continue;
                    }

                    details.Append("\n  ").Append(line.TrimEnd());
                }
            }

            if (headline.Length == 0) return null;

            return headline.Append(details).ToString();
        }
    }
}
