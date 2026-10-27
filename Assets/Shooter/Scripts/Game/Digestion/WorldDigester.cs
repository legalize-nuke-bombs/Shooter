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

            foreach (MainDigestible entity in FindVisibleEntities(observer, origin))
            {
                string entityDigest = Digester.DigestEntity(entity, DigestionDetail.Brief);
                if (entityDigest != null) digest.Append(entityDigest).Append('\n');
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
