using System.Collections.Generic;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core
{
    public class PersistentIds : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        public const long Nobody = -1;

        private readonly Dictionary<long, PersistentId> known = new Dictionary<long, PersistentId>();

        private long counter;

        public long Reserve()
        {
            return counter++;
        }

        public void Register(PersistentId id)
        {
            if (known.TryGetValue(id.Value, out PersistentId taken) && taken != id)
            {
                Log.Error($"Entities {taken.name} and {id.name} share the persistent id {id.Value}, the second one stays unreachable");
                return;
            }

            known[id.Value] = id;
        }

        public void Forget(long value, PersistentId id)
        {
            if (known.TryGetValue(value, out PersistentId taken) && taken == id) known.Remove(value);
        }

        public PersistentId Of(long id)
        {
            return known.TryGetValue(id, out PersistentId found) ? found : null;
        }

        public List<PersistentId> GetFiltered(string layerName)
        {
            int targetLayer = LayerMask.NameToLayer(layerName);
            if (targetLayer == -1)
            {
                Log.Error($"Layer with name '{layerName}' does not exist!");
                return new List<PersistentId>();
            }

            var result = new List<PersistentId>();

            foreach (PersistentId id in known.Values)
            {
                if (id.gameObject.layer == targetLayer)
                {
                    result.Add(id);
                }
            }

            return result;
        }
    }
}
