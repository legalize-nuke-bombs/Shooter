using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(Digester))]
    public class WorldDigester : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private float viewingRadius = 500f;

        private Digester digester;

        private void Awake()
        {
            digester = GetComponent<Digester>();
        }

        public string Digest(Vector3? position = null)
        {
            position ??= transform.position;

            HashSet<GameObject> objects = FindNearObjects(position.Value);
            List<GameObject> sortedObjects = SortObjects(objects);

            var digest = new StringBuilder();

            foreach (GameObject nearObject in sortedObjects)
            {
                string seen = digester.Seen(nearObject, DigestionDetail.Brief, transform);
                if (seen != null) digest.Append(seen).Append('\n');
            }

            string result = digest.ToString();
            Log.Info($"Digestion finished, length: {result.Length}");
            return result;
        }

        private HashSet<GameObject> FindNearObjects(Vector3 position)
        {
            /*
            List<GameObject> Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .ConvertAll(x => x.GetComponent<IMyInterface>()?.transform.parent?.gameObject)
                .FindAll(x => x != null);
            List<GameObject> parents = GameObject.FindObjectsByType<MonoBehaviour>()
                .OfType<IDigestible>()
                .Select(component => ((MonoBehaviour)component).transform.parent?.gameObject)
                .Where(parent => parent != null)
                .Distinct()
                .ToList();

            var found = new HashSet<GameObject>();
            int hits = Physics.OverlapSphereNonAlloc(position, viewingRadius, Around);
            if (hits == Around.Length)
                Log.Warn($"Digester of {name} filled its buffer of {Around.Length} colliders within {viewingRadius} m, the digest may miss part of the world");

            for (int i = 0; i < hits; i++)
            {
                if (!(Around[i].GetComponentInParent<IDigestible>() is Component owner)) continue;
                if (owner.gameObject == gameObject) continue;

                GameObject target = owner.gameObject;
                if (found.Contains(target))
                {
                    continue;
                }

                float targetDistanceScore = digester.DistanceScore(target);
                if ((position - target.transform.position).sqrMagnitude * targetDistanceScore > viewingRadius * viewingRadius)
                {
                    continue;
                }

                found.Add(owner.gameObject);
            }

            return found;*/
            return null;
            // TODO
        }

        private List<GameObject> SortObjects(HashSet<GameObject> objects)
        {
            return objects.OrderBy(owner => (transform.position - owner.transform.position).sqrMagnitude)
                .ToList();
        }
    }
}
