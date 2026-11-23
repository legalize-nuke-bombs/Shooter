using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public static class WorldDigester
    {
        private const float SmallViewingDistance = 25f;
        private const float MediumViewingDistance = 100f;
        private const float LargeViewingDistance = 250f;
        private const float BiggestViewingDistance = 1000f;
        private static readonly Journal Log = Logs.Here();

        public static string DigestVisibleAround(GameObject observer)
        {
            Vector3 origin = observer.transform.position;

            StringBuilder digest = new();

            foreach (DigestedGroup group in GroupAlike(FindVisibleEntities(observer, origin)))
            {
                string groupDigest = group.Count == 1
                    ? Digester.DigestEntity(group.Nearest, DigestionDetail.Brief)
                    : Digester.DigestGroup(group.Nearest, DigestionDetail.Brief, group.Count);
                if (groupDigest != null) digest.Append(groupDigest).Append('\n');
            }

            string result = digest.ToString();
            Log.Info($"Digestion around {observer.name} finished, length: {result.Length}");
            return result;
        }

        private static List<MainDigestible> FindVisibleEntities(GameObject observer, Vector3 origin)
        {
            var visibleEntities = new List<MainDigestible>();

            foreach (MainDigestible entity in Registers.Instance.GetAll<MainDigestible>(RegisterInactivePolicy.Exclude))
            {
                if (entity.gameObject == observer) continue;

                float viewingDistance = GetViewingDistance(entity.Size);
                if ((origin - entity.transform.position).sqrMagnitude > viewingDistance * viewingDistance) continue;

                visibleEntities.Add(entity);
            }

            return visibleEntities.OrderBy(entity => (origin - entity.transform.position).sqrMagnitude).ToList();
        }

        private sealed class DigestedGroup
        {
            public MainDigestible Nearest;
            public int Count;
        }

        private static List<DigestedGroup> GroupAlike(List<MainDigestible> entitiesByDistance)
        {
            var groups = new List<DigestedGroup>();
            var groupsByText = new Dictionary<string, DigestedGroup>();

            foreach (MainDigestible entity in entitiesByDistance)
            {
                string text = Digester.DigestWithoutPlace(entity, DigestionDetail.Brief);
                if (text != null && groupsByText.TryGetValue(text, out DigestedGroup alike))
                {
                    alike.Count++;
                    continue;
                }

                var group = new DigestedGroup { Nearest = entity, Count = 1 };
                groups.Add(group);
                if (text != null) groupsByText.Add(text, group);
            }

            return groups;
        }

        private static float GetViewingDistance(DigestibleSize size)
        {
            return size switch
            {
                DigestibleSize.Biggest => BiggestViewingDistance,
                DigestibleSize.Large => LargeViewingDistance,
                DigestibleSize.Medium => MediumViewingDistance,
                _ => SmallViewingDistance
            };
        }
    }
}
