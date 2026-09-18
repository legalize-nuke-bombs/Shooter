using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm
{
    public static class WorldDigester
    {
        private const float SmallViewingDistance = 50f;
        private const float MediumViewingDistance = 150f;
        private const float LargeViewingDistance = 500f;
        private const float BiggestViewingDistance = 5000f;
        private static readonly Journal Log = Logs.Here();

        public static string Digest(GameObject around)
        {
            Vector3 origin = around.transform.position;

            var digest = new StringBuilder();

            foreach (MainDigestible entity in FindVisible(around, origin))
            {
                string seen = Digester.Of(entity, DigestionDetail.Brief);
                if (seen != null) digest.Append(seen).Append('\n');
            }

            string result = digest.ToString();
            Log.Info($"Digestion around {around.name} finished, length: {result.Length}");
            return result;
        }

        private static List<MainDigestible> FindVisible(GameObject around, Vector3 origin)
        {
            var visible = new List<MainDigestible>();

            foreach (MainDigestible entity in Registers.Of<MainDigestible>(Inactive.Exclude))
            {
                if (entity.gameObject == around) continue;

                float reach = ViewingDistance(entity.Size);
                if ((origin - entity.transform.position).sqrMagnitude > reach * reach) continue;

                visible.Add(entity);
            }

            return visible.OrderBy(entity => (origin - entity.transform.position).sqrMagnitude).ToList();
        }

        private static float ViewingDistance(DigestibleSize size)
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
