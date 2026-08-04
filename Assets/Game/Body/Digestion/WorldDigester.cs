using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Shooter.Game.Body
{
    [RequireComponent(typeof(Digester))]
    public class WorldDigester : MonoBehaviour
    {
        [SerializeField] private float scanRadius = 250f;

        private Digester digester;

        public void Awake()
        {
            digester = GetComponent<Digester>();
        }

        public string DigestNearObjects()
        {
            var digest = new StringBuilder();

            foreach (Component nearObject in FindNearObjects())
            {
                string seen = digester.Seen(nearObject, DigestionDetail.Brief, transform);
                if (seen != null) digest.Append(seen).Append('\n');
            }

            return digest.ToString();
        }

        private List<Component> FindNearObjects()
        {
            var found = new HashSet<Component>();
            Collider[] hits = Physics.OverlapSphere(transform.position, scanRadius);

            foreach (Collider hit in hits)
            {
                if (!(hit.GetComponentInParent<IDigestible>() is Component owner)) continue;
                if (owner.gameObject == gameObject) continue;

                found.Add(owner);
            }

            return found.OrderBy(owner => (transform.position - owner.transform.position).sqrMagnitude)
                .ToList();
        }
    }
}
