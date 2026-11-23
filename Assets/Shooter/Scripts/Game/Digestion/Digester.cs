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
            return Compose(mainDigestible, detail, true, null);
        }

        public static string DigestWithoutPlace(MainDigestible mainDigestible, DigestionDetail detail)
        {
            return Compose(mainDigestible, detail, false, null);
        }

        public static string DigestGroup(MainDigestible nearest, DigestionDetail detail, int count)
        {
            return Compose(nearest, detail, true, $"×{count}, nearest");
        }

        private static string Compose(MainDigestible mainDigestible, DigestionDetail detail, bool withPlace, string placeLead)
        {
            if (mainDigestible == null) return null;

            StringBuilder headline = new();
            StringBuilder details = new();

            IncrementalId incrementalId = mainDigestible.GetComponent<IncrementalId>();
            if (incrementalId != null) headline.Append("[ID ").Append(incrementalId.Value).Append(']');

            foreach (IDigestible part in mainDigestible.Parts)
            {
                bool isPlace = part.Priority == DigestionPriority.Place;
                if (isPlace && !withPlace) continue;

                string partDigest = part.Digest(detail);
                if (string.IsNullOrWhiteSpace(partDigest)) continue;

                bool isNextLineHeadline = part.Priority >= DigestionPriority.Place;

                foreach (string line in partDigest.Trim().Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (isNextLineHeadline)
                    {
                        if (headline.Length > 0) headline.Append(' ');
                        if (isPlace && placeLead != null) headline.Append(placeLead).Append(' ');
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
