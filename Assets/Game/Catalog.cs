using System.Collections.Generic;
using Shooter.Logging;
using Unity.Collections;
using UnityEngine;

namespace Shooter.Game
{
    public abstract class Catalog<TSpec> : ScriptableObject where TSpec : Spec
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

            if (unknown.Add(id)) Log.Warn("Catalog {} has nothing under id {}", name, id);

            return null;
        }

        private void OnEnable()
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
                    Log.Error("Catalog {} skips {}: its id does not fit the network format", name, spec.name);
                    continue;
                }

                if (known.TryGetValue(spec.Id, out TSpec taken))
                {
                    Log.Error("Catalog {} holds both {} and {} under id {}", name, taken.name, spec.name, spec.Key);
                    continue;
                }

                known.Add(spec.Id, spec);
                indices.Add(spec.Id, ordered.Count);
                ordered.Add(spec);
            }

            Log.Info("Catalog {} knows {} things", name, known.Count);
        }

        public TSpec Find(System.Func<TSpec, bool> predicate)
        {
            if (predicate == null) return null;

            for (int i = 0; i < ordered.Count; i++)
            {
                if (predicate(ordered[i]))
                {
                    return ordered[i];
                }
            }

            return null;
        }
    }
}
