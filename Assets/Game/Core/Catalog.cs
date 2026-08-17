using System.Collections.Generic;
using Shooter.Logging;
using Unity.Collections;
using UnityEngine;

namespace Shooter.Game.Core
{
    public abstract class Catalog : ScriptableObject
    {
    }

    public abstract class Catalog<TSpec> : Catalog where TSpec : Spec
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private TSpec[] specs;

        private readonly Dictionary<FixedString32Bytes, TSpec> known = new Dictionary<FixedString32Bytes, TSpec>();
        private readonly HashSet<FixedString32Bytes> unknown = new HashSet<FixedString32Bytes>();

        private readonly List<TSpec> ordered = new List<TSpec>();
        private readonly Dictionary<FixedString32Bytes, int> indices = new Dictionary<FixedString32Bytes, int>();

        public int Count => ordered.Count;

        public TSpec At(int index)
        {
            return index >= 0 && index < ordered.Count ? ordered[index] : null;
        }

        public int Index(TSpec spec)
        {
            return spec != null && indices.TryGetValue(spec.Id, out int index) ? index : -1;
        }

        public TSpec Of(FixedString32Bytes id)
        {
            if (known.TryGetValue(id, out TSpec spec)) return spec;

            if (unknown.Add(id)) Log.Warn($"Catalog {name} has nothing under id {id}");

            return null;
        }

        protected virtual void OnEnable()
        {
            known.Clear();
            unknown.Clear();
            ordered.Clear();
            indices.Clear();

            if (specs == null) return;

            foreach (TSpec spec in specs)
            {
                if (spec == null) continue;

                if (!spec.Fits())
                {
                    Log.Error($"Catalog {name} skips {spec.name}: its id does not fit the network format");
                    continue;
                }

                if (known.TryGetValue(spec.Id, out TSpec taken))
                {
                    Log.Error($"Catalog {name} holds both {taken.name} and {spec.name} under id {spec.Key}");
                    continue;
                }

                known.Add(spec.Id, spec);
                indices.Add(spec.Id, ordered.Count);
                ordered.Add(spec);
            }

            Log.Info($"Catalog {name} knows {known.Count} things");
        }

        public List<TSpec> FindAll(System.Func<TSpec, bool> predicate)
        {
            if (predicate == null) return null;

            var result = new List<TSpec>();

            for (int i = 0; i < ordered.Count; i++)
            {
                if (predicate(ordered[i]))
                {
                    result.Add(ordered[i]);
                }
            }

            return result;
        }
    }
}
