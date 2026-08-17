using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shooter.Logging;
using UnityEngine;
using Shooter.Game.Core;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(Digester))]
    public class WorldDigester : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private float smallViewingDistance = 50f;
        [SerializeField] private float mediumViewingDistance = 150f;
        [SerializeField] private float largeViewingDistance = 500f;
        [SerializeField] private float biggestViewingDistance = 1e+9f;

        private Digester digester;

        private void Awake()
        {
            digester = GetComponent<Digester>();
        }

        public string Digest(Vector3? position = null)
        {
            Vector3 origin = position ?? transform.position;

            var digest = new StringBuilder();

            foreach (MainDigestible entity in FindVisible(origin))
            {
                string seen = digester.Seen(entity, DigestionDetail.Brief, origin);
                if (seen != null) digest.Append(seen).Append('\n');
            }

            string result = digest.ToString();
            Log.Info($"Digestion finished, length: {result.Length}");
            return result;
        }

        private List<MainDigestible> FindVisible(Vector3 origin)
        {
            var visible = new List<MainDigestible>();

            foreach (MainDigestible entity in Registers.Current.Of<MainDigestible>().All)
            {
                if (entity.gameObject == gameObject) continue;

                float reach = ViewingDistance(entity.Size);
                if ((origin - entity.transform.position).sqrMagnitude > reach * reach) continue;

                visible.Add(entity);
            }

            return visible.OrderBy(entity => (origin - entity.transform.position).sqrMagnitude).ToList();
        }

        private float ViewingDistance(DigestibleSize size)
        {
            return size switch
            {
                DigestibleSize.Biggest => biggestViewingDistance,
                DigestibleSize.Large => largeViewingDistance,
                DigestibleSize.Medium => mediumViewingDistance,
                _ => smallViewingDistance,
            };
        }
    }
}
