using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Body
{
    [RequireComponent(typeof(Digester))]
    public class WorldDigester : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly Collider[] Around = new Collider[8192];

        [SerializeField] private float scanRadius = 250f;

        private Digester digester;

        private void Awake()
        {
            digester = GetComponent<Digester>();
        }

        public string Digest()
        {
            var digest = new StringBuilder();

            foreach (GameObject nearObject in FindNearObjects())
            {
                string seen = digester.Seen(nearObject, DigestionDetail.Brief, transform);
                if (seen != null) digest.Append(seen).Append('\n');
            }

            string result = digest.ToString();
            Log.Info("Digestion finished, length: {}", result.Length);
            return result;
        }

        private List<GameObject> FindNearObjects()
        {
            var found = new HashSet<GameObject>();
            int hits = Physics.OverlapSphereNonAlloc(transform.position, scanRadius, Around);
            if (hits == Around.Length)
                Log.Warn("Digester of {} filled its buffer of {} colliders within {} m, the digest may miss part of the world", name, Around.Length, scanRadius);

            for (int i = 0; i < hits; i++)
            {
                if (!(Around[i].GetComponentInParent<IDigestible>() is Component owner)) continue;
                if (owner.gameObject == gameObject) continue;

                found.Add(owner.gameObject);
            }

            return found.OrderBy(owner => (transform.position - owner.transform.position).sqrMagnitude)
                .ToList();
        }
    }
}
