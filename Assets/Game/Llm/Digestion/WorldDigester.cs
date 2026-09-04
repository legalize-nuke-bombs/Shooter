using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm
{
    [DefaultExecutionOrder(ExecutionOrder.Service)]
    public class WorldDigester : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private float smallViewingDistance = 50f;
        [SerializeField] private float mediumViewingDistance = 150f;
        [SerializeField] private float largeViewingDistance = 500f;
        [SerializeField] private float biggestViewingDistance = 1e+9f;

        public static WorldDigester Current { get; private set; }

        private void Awake()
        {
            if (Current != null)
            {
                Log.Error("Singleton class has more than one instance");
            }
            Current = this;
        }

        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

        public string Digest(GameObject around)
        {
            Vector3 origin = around.transform.position;

            var digest = new StringBuilder();

            foreach (MainDigestible entity in FindVisible(around, origin))
            {
                string seen = Digester.Current.Seen(entity, DigestionDetail.Brief, origin);
                if (seen != null) digest.Append(seen).Append('\n');
            }

            string result = digest.ToString();
            Log.Info($"Digestion around {around.name} finished, length: {result.Length}");
            return result;
        }

        private List<MainDigestible> FindVisible(GameObject around, Vector3 origin)
        {
            var visible = new List<MainDigestible>();

            foreach (MainDigestible entity in Registers.Current.Of<MainDigestible>(Inactive.Exclude))
            {
                if (entity.gameObject == around) continue;

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
                _ => smallViewingDistance
            };
        }
    }
}
